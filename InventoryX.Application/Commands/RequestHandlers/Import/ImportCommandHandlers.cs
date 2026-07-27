using System.Globalization;
using System.Text.Json;
using InventoryX.Application.Commands.Requests.Import;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Import
{
    internal static class ImportJobMapping
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public static ImportJobDto ToDto(ImportJob job, List<ImportRowResult>? preview = null) => new(
            job.Id,
            job.Kind.ToString(),
            job.FileName,
            job.Status.ToString(),
            JsonSerializer.Deserialize<List<string>>(job.DetectedColumns, Options) ?? [],
            preview ?? (job.RowResults is null
                ? null
                : JsonSerializer.Deserialize<List<ImportRowResult>>(job.RowResults, Options)),
            job.CreatedCount,
            job.UpdatedCount,
            job.SkippedCount);

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        public static Dictionary<string, string>? DeserializeMapping(string? json) =>
            json is null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options);

        public static List<ImportRowResult> DeserializeRows(string? json) =>
            json is null ? [] : JsonSerializer.Deserialize<List<ImportRowResult>>(json, Options) ?? [];
    }

    public class CreateImportJobCommandHandler(IAppDbContext context, ISpreadsheetImportService importService)
        : IRequestHandler<CreateImportJobCommand, ImportJobDto>
    {
        public async Task<ImportJobDto> Handle(CreateImportJobCommand request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<ImportKind>(request.Kind, true, out var kind))
                throw new FluentValidation.ValidationException("Import kind must be Products or OpeningStock.");
            if (request.FileContent.Length == 0)
                throw new FluentValidation.ValidationException("The uploaded file is empty.");

            var columns = importService.DetectColumns(request.FileContent, request.FileName);
            var job = new ImportJob
            {
                Kind = kind,
                FileName = request.FileName,
                FileContent = request.FileContent,
                DetectedColumns = ImportJobMapping.Serialize(columns),
            };
            context.ImportJobs.Add(job);
            await context.SaveChangesAsync(cancellationToken);
            return ImportJobMapping.ToDto(job);
        }
    }

    public class SetImportMappingCommandHandler(IAppDbContext context, ISpreadsheetImportService importService)
        : IRequestHandler<SetImportMappingCommand, ImportJobDto>
    {
        public async Task<ImportJobDto> Handle(SetImportMappingCommand request, CancellationToken cancellationToken)
        {
            var job = await context.ImportJobs.FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
                ?? throw new NotFoundException("Import job not found.");
            if (job.Status is ImportJobStatus.Committed or ImportJobStatus.Abandoned)
                throw new ConflictException($"Import job is already {job.Status}.");

            var rows = importService.ParseRows(job.FileContent, job.FileName, request.ColumnMapping, job.Kind.ToString());
            job.ColumnMapping = ImportJobMapping.Serialize(request.ColumnMapping);
            job.RowResults = ImportJobMapping.Serialize(rows);
            job.Status = ImportJobStatus.Previewed;
            await context.SaveChangesAsync(cancellationToken);
            return ImportJobMapping.ToDto(job, rows);
        }
    }

    public class CommitImportCommandHandler(IAppDbContext context, IStockLedger stockLedger)
        : IRequestHandler<CommitImportCommand, ImportJobDto>
    {
        public async Task<ImportJobDto> Handle(CommitImportCommand request, CancellationToken cancellationToken)
        {
            var job = await context.ImportJobs.FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
                ?? throw new NotFoundException("Import job not found.");
            if (job.Status != ImportJobStatus.Previewed)
                throw new ConflictException("Set a column mapping (preview) before committing.");

            var rows = ImportJobMapping.DeserializeRows(job.RowResults);
            if (job.Kind == ImportKind.Products)
                await CommitProductsAsync(job, rows, cancellationToken);
            else
                await CommitOpeningStockAsync(job, rows, request.LocationId, cancellationToken);

            job.Status = ImportJobStatus.Committed;
            job.FileContent = [];
            await context.SaveChangesAsync(cancellationToken);
            return ImportJobMapping.ToDto(job);
        }

        private async Task CommitProductsAsync(ImportJob job, List<ImportRowResult> rows, CancellationToken cancellationToken)
        {
            foreach (var row in rows)
            {
                if (!row.IsValid) { job.SkippedCount++; continue; }

                var sku = row.Values.GetValueOrDefault("sku");
                var name = row.Values.GetValueOrDefault("name")!;
                decimal.TryParse(row.Values.GetValueOrDefault("sellingPrice"), NumberStyles.Number, CultureInfo.InvariantCulture, out var sellingPrice);
                decimal.TryParse(row.Values.GetValueOrDefault("costPrice"), NumberStyles.Number, CultureInfo.InvariantCulture, out var costPrice);

                var existing = string.IsNullOrWhiteSpace(sku)
                    ? null
                    : await context.Products.FirstOrDefaultAsync(p => p.Sku == sku && !p.IsDeleted, cancellationToken);

                if (existing is not null)
                {
                    existing.Name = name;
                    if (sellingPrice > 0) existing.SellingPrice = sellingPrice;
                    if (costPrice > 0) existing.CostPrice = costPrice;
                    job.UpdatedCount++;
                    continue;
                }

                Guid? categoryId = null;
                var categoryName = row.Values.GetValueOrDefault("category");
                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var category = await context.Categories
                        .FirstOrDefaultAsync(c => c.Name == categoryName && c.ParentId == null && !c.IsDeleted, cancellationToken);
                    if (category is null)
                    {
                        category = new Category { Name = categoryName };
                        context.Categories.Add(category);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    categoryId = category.Id;
                }

                decimal? reorderPoint = null;
                if (decimal.TryParse(row.Values.GetValueOrDefault("reorderPoint"), NumberStyles.Number, CultureInfo.InvariantCulture, out var rp))
                    reorderPoint = rp;

                context.Products.Add(new Product
                {
                    Name = name,
                    Sku = sku,
                    Barcode = row.Values.GetValueOrDefault("barcode"),
                    CategoryId = categoryId,
                    SellingPrice = sellingPrice,
                    CostPrice = costPrice,
                    ReorderPoint = reorderPoint,
                });
                job.CreatedCount++;
            }
        }

        private async Task CommitOpeningStockAsync(
            ImportJob job, List<ImportRowResult> rows, Guid? locationId, CancellationToken cancellationToken)
        {
            var location = locationId is null
                ? await context.Locations.OrderBy(l => l.CreatedAt).FirstOrDefaultAsync(l => !l.IsDeleted, cancellationToken)
                : await context.Locations.FirstOrDefaultAsync(l => l.Id == locationId && !l.IsDeleted, cancellationToken);
            if (location is null) throw new NotFoundException("Target location not found.");

            var correlationId = Guid.NewGuid();
            foreach (var row in rows)
            {
                if (!row.IsValid) { job.SkippedCount++; continue; }

                var sku = row.Values.GetValueOrDefault("sku");
                var barcode = row.Values.GetValueOrDefault("barcode");
                var product = await context.Products.FirstOrDefaultAsync(p => !p.IsDeleted &&
                    ((sku != null && p.Sku == sku) || (barcode != null && p.Barcode == barcode)), cancellationToken);
                if (product is null) { job.SkippedCount++; continue; }

                decimal.TryParse(row.Values.GetValueOrDefault("qty"), NumberStyles.Number, CultureInfo.InvariantCulture, out var qty);
                decimal? unitCost = null;
                if (decimal.TryParse(row.Values.GetValueOrDefault("unitCost"), NumberStyles.Number, CultureInfo.InvariantCulture, out var cost))
                    unitCost = cost;

                await stockLedger.AppendAsync([new StockMovementRequest(
                    MovementType.Adjustment, product.Id, location.Id, qty,
                    UnitCost: unitCost, ReasonCode: "Correction", Note: $"Opening stock import {job.Id}",
                    CorrelationId: correlationId, AllowNegative: true)], cancellationToken);
                job.CreatedCount++;
            }
        }
    }

    public class AbandonImportCommandHandler(IAppDbContext context) : IRequestHandler<AbandonImportCommand, bool>
    {
        public async Task<bool> Handle(AbandonImportCommand request, CancellationToken cancellationToken)
        {
            var job = await context.ImportJobs.FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
                ?? throw new NotFoundException("Import job not found.");
            if (job.Status == ImportJobStatus.Committed)
                throw new ConflictException("Committed jobs cannot be abandoned.");
            job.Status = ImportJobStatus.Abandoned;
            job.FileContent = [];
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
