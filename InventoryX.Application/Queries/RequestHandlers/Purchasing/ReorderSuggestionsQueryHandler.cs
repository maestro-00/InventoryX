using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Purchasing;

public sealed class ReorderSuggestionsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetReorderSuggestionsQuery, ReorderSuggestionsDto>
{
    public async Task<ReorderSuggestionsDto> Handle(GetReorderSuggestionsQuery request, CancellationToken cancellationToken)
    {
        // Get stock levels at or below reorder point
        var stockQuery = context.StockLevels
            .AsNoTracking()
            .Where(sl => sl.BatchId == null); // aggregate at product level

        if (request.LocationId.HasValue)
            stockQuery = stockQuery.Where(sl => sl.LocationId == request.LocationId.Value);

        var stockLevels = await stockQuery
            .GroupBy(sl => new { sl.ProductId, sl.VariantId })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.VariantId,
                TotalQty = g.Sum(sl => sl.QtyOnHand),
            })
            .ToListAsync(cancellationToken);

        var productIds = stockLevels.Select(sl => sl.ProductId).Distinct().ToList();

        var products = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.ReorderPoint.HasValue && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        // Get supplier links (first supplier per product)
        var suppliers = await context.Suppliers
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var supplierDict = suppliers.ToDictionary(s => s.Id);

        // Get PO lines to find preferred supplier per product
        var poLines = await context.PurchaseOrderLines
            .AsNoTracking()
            .Join(context.PurchaseOrders.AsNoTracking(),
                l => l.PurchaseOrderId,
                po => po.Id,
                (l, po) => new { l.ProductId, po.SupplierId, l.UnitCost })
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, SupplierId = g.First().SupplierId, UnitCost = g.Average(x => x.UnitCost) })
            .ToListAsync(cancellationToken);

        var supplierByProduct = poLines.ToDictionary(x => x.ProductId);

        var suggestions = new List<ReorderSuggestionItem>();
        foreach (var product in products)
        {
            var stock = stockLevels.FirstOrDefault(sl => sl.ProductId == product.Id);
            var currentQty = stock?.TotalQty ?? 0;

            if (currentQty > product.ReorderPoint!.Value) continue;

            var supplierInfo = supplierByProduct.TryGetValue(product.Id, out var si) ? si : null;
            var supplier = supplierInfo != null && supplierDict.TryGetValue(supplierInfo.SupplierId, out var s) ? s : null;

            // Suggested qty = ReorderQuantity or (ReorderPoint * 2) as fallback
            var suggestedQty = product.ReorderQuantity ?? (product.ReorderPoint.Value * 2);

            suggestions.Add(new ReorderSuggestionItem(
                product.Id,
                product.Name,
                product.Sku,
                supplier?.Id,
                supplier?.Name,
                currentQty,
                product.ReorderPoint.Value,
                suggestedQty,
                product.LeadTimeDays,
                supplierInfo?.UnitCost ?? product.CostPrice));
        }

        return new ReorderSuggestionsDto(suggestions.OrderBy(s => s.SupplierName).ThenBy(s => s.ProductName).ToList());
    }
}
