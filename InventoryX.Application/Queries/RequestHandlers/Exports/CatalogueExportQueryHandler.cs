using InventoryX.Application.Queries.Requests.Exports;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Exports;

public sealed class ExportCatalogueQueryHandler(IAppDbContext context, IReportExportService exporter)
    : IRequestHandler<ExportCatalogueQuery, ReportExportDocument>
{
    public async Task<ReportExportDocument> Handle(ExportCatalogueQuery request, CancellationToken cancellationToken)
    {
        var rows = request.Resource.Trim().ToLowerInvariant() switch
        {
            "products" => await ProductRows(request.IncludeCost, cancellationToken),
            "stock" => await StockRows(request.IncludeCost, cancellationToken),
            _ => throw new FluentValidation.ValidationException("Export resource must be products or stock."),
        };

        return await exporter.GenerateAsync(request.Resource, request.Format, rows, cancellationToken);
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ProductRows(
        bool includeCost, CancellationToken cancellationToken)
    {
        var products = await context.Products.AsNoTracking()
            .Where(product => !product.IsDeleted)
            .OrderBy(product => product.Name)
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.Sku,
                product.Barcode,
                product.CategoryId,
                product.UnitOfMeasure,
                product.SellingPrice,
                product.CostPrice,
                product.Status,
                product.TrackingMode,
                product.ReorderPoint,
                product.ReorderQuantity,
            })
            .ToListAsync(cancellationToken);

        return products.Select(product => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["id"] = product.Id,
            ["name"] = product.Name,
            ["sku"] = product.Sku,
            ["barcode"] = product.Barcode,
            ["categoryId"] = product.CategoryId,
            ["unitOfMeasure"] = product.UnitOfMeasure.ToString(),
            ["sellingPrice"] = product.SellingPrice,
            ["costPrice"] = includeCost ? product.CostPrice : null,
            ["status"] = product.Status.ToString(),
            ["trackingMode"] = product.TrackingMode.ToString(),
            ["reorderPoint"] = product.ReorderPoint,
            ["reorderQuantity"] = product.ReorderQuantity,
        }).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StockRows(
        bool includeCost, CancellationToken cancellationToken)
    {
        var rows = await (from stock in context.StockLevels.AsNoTracking()
                          join product in context.Products.AsNoTracking() on stock.ProductId equals product.Id
                          join location in context.Locations.AsNoTracking() on stock.LocationId equals location.Id
                          where !product.IsDeleted && !location.IsDeleted
                          orderby product.Name, location.Name
                          select new
                          {
                              stock.ProductId,
                              ProductName = product.Name,
                              stock.VariantId,
                              stock.BatchId,
                              stock.LocationId,
                              LocationName = location.Name,
                              stock.QtyOnHand,
                              stock.QtyInTransit,
                              stock.QtyQuarantine,
                              stock.AvgUnitCost,
                          }).ToListAsync(cancellationToken);

        return rows.Select(row => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["productId"] = row.ProductId,
            ["productName"] = row.ProductName,
            ["variantId"] = row.VariantId,
            ["batchId"] = row.BatchId,
            ["locationId"] = row.LocationId,
            ["locationName"] = row.LocationName,
            ["qtyOnHand"] = row.QtyOnHand,
            ["qtyInTransit"] = row.QtyInTransit,
            ["qtyQuarantine"] = row.QtyQuarantine,
            ["avgUnitCost"] = includeCost ? row.AvgUnitCost : null,
        }).ToList();
    }
}
