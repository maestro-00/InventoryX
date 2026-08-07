using System.Text.Json;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Reports;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Reports;

public sealed class ExportReportCommandHandler(ISender sender, IReportExportService exporter, IAppDbContext context)
    : IRequestHandler<ExportReportCommand, ReportExportResult>
{
    public async Task<ReportExportResult> Handle(ExportReportCommand request, CancellationToken cancellationToken)
    {
        object report = request.ReportType.ToLowerInvariant() switch
        {
            "sales" => await sender.Send(new GetSalesReportQuery(request.Filter), cancellationToken),
            "profit" => await sender.Send(new GetProfitReportQuery(request.Filter), cancellationToken),
            "stock" => await sender.Send(new GetStockReportQuery(request.Filter), cancellationToken),
            "purchasing" => await sender.Send(new GetPurchasingReportQuery(request.Filter), cancellationToken),
            "staff" => await sender.Send(new GetStaffReportQuery(request.Filter), cancellationToken),
            "tax" => await sender.Send(new GetTaxReportQuery(request.Filter.From, request.Filter.To,
                request.Filter.LocationId, request.Filter.CategoryId, request.Filter.StaffId), cancellationToken),
            _ => throw new FluentValidation.ValidationException("Unknown report type."),
        };
        var rows = ToRows(report);
        var document = await exporter.GenerateAsync(request.ReportType, request.Format, rows, cancellationToken);
        if ((request.Filter.To - request.Filter.From).TotalDays <= 31)
            return new ReportExportResult(null, ReportExportStatus.Completed, false, document.FileName, document.ContentType, document.Content);
        var job = new ReportExportJob
        {
            ReportType = request.ReportType, Format = request.Format, Status = ReportExportStatus.Completed,
            FileName = document.FileName, ContentType = document.ContentType, Content = document.Content,
            CompletedAt = DateTime.UtcNow,
        };
        context.ReportExportJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);
        return new ReportExportResult(job.Id, job.Status, true);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ToRows(object report)
    {
        var root = JsonSerializer.SerializeToElement(report, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        JsonElement array = default;
        if ((!root.TryGetProperty("rows", out array) && !root.TryGetProperty("components", out array)) || array.ValueKind != JsonValueKind.Array)
            return [];
        return array.EnumerateArray().Select(item => (IReadOnlyDictionary<string, object?>)item.EnumerateObject()
            .ToDictionary(property => property.Name, property => Scalar(property.Value), StringComparer.Ordinal)).ToList();
    }

    private static object? Scalar(JsonElement value) => value.ValueKind switch
    { JsonValueKind.String when value.TryGetDateTime(out var date) => date, JsonValueKind.String => value.GetString(),
      JsonValueKind.Number when value.TryGetDecimal(out var number) => number, JsonValueKind.True => true,
      JsonValueKind.False => false, JsonValueKind.Null => null, _ => value.ToString() };
}

public sealed class GetReportExportJobQueryHandler(IAppDbContext context) : IRequestHandler<GetReportExportJobQuery, ReportExportResult>
{
    public async Task<ReportExportResult> Handle(GetReportExportJobQuery request, CancellationToken cancellationToken)
    {
        var job = await context.ReportExportJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.JobId, cancellationToken)
            ?? throw new NotFoundException("Report export job not found.");
        return new ReportExportResult(job.Id, job.Status, job.Status == ReportExportStatus.Pending,
            job.FileName, job.ContentType, job.Content, job.Error);
    }
}
