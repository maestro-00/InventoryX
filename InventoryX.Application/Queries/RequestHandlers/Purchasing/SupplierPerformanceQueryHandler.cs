using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Purchasing;

public sealed class SupplierPerformanceQueryHandler(IAppDbContext context)
    : IRequestHandler<GetSupplierPerformanceQuery, SupplierPerformanceDto>
{
    public async Task<SupplierPerformanceDto> Handle(GetSupplierPerformanceQuery request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new NotFoundException("Supplier not found.");

        // Load completed purchase orders for this supplier
        var orders = await context.PurchaseOrders
            .AsNoTracking()
            .Where(po => po.SupplierId == request.SupplierId &&
                         po.Status != PurchaseOrderStatus.Draft &&
                         po.Status != PurchaseOrderStatus.Cancelled)
            .ToListAsync(cancellationToken);

        // Load goods receipts for this supplier
        var receipts = await context.GoodsReceipts
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.SupplierId == request.SupplierId)
            .ToListAsync(cancellationToken);

        // On-time rate: orders with RequiredBy where receipt arrived on or before RequiredBy
        var ordersWithDeadline = orders.Where(po => po.RequiredBy.HasValue).ToList();
        var onTimeCount = 0;
        var leadTimes = new List<double>();

        foreach (var order in ordersWithDeadline)
        {
            var orderReceipts = receipts.Where(r => r.PurchaseOrderId == order.Id).ToList();
            if (orderReceipts.Count == 0) continue;

            var firstReceiptDate = orderReceipts.Min(r => r.ReceivedAt);
            if (firstReceiptDate <= order.RequiredBy!.Value)
                onTimeCount++;

            if (order.SentAt.HasValue)
                leadTimes.Add((firstReceiptDate - order.SentAt.Value).TotalDays);
        }

        var totalOrdersWithDeadline = ordersWithDeadline.Count(po =>
            receipts.Any(r => r.PurchaseOrderId == po.Id));

        var onTimeRate = totalOrdersWithDeadline > 0
            ? (double)onTimeCount / totalOrdersWithDeadline * 100.0
            : 0.0;

        var avgLeadTime = leadTimes.Count > 0 ? leadTimes.Average() : (double?)null;

        // Price history from goods receipt lines
        var receiptIds = receipts.Select(r => r.Id).ToList();
        var productIds = receipts.SelectMany(r => r.Lines).Select(l => l.ProductId).Distinct().ToList();
        var products = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var priceHistory = receipts
            .SelectMany(r => r.Lines.Select(l => new SupplierPriceHistoryItem(
                l.ProductId,
                products.TryGetValue(l.ProductId, out var name) ? name : l.ProductId.ToString(),
                l.UnitCost,
                r.ReceivedAt)))
            .OrderByDescending(ph => ph.ReceivedAt)
            .Take(50)
            .ToList();

        return new SupplierPerformanceDto(
            supplier.Id,
            supplier.Name,
            orders.Count,
            onTimeCount,
            Math.Round(onTimeRate, 1),
            avgLeadTime.HasValue ? Math.Round(avgLeadTime.Value, 1) : null,
            priceHistory);
    }
}
