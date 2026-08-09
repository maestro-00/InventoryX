using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Purchasing;

public sealed class RecordGoodsReceiptCommandHandler(IAppDbContext context, IStockLedger stockLedger, ITenantContext tenantContext)
    : IRequestHandler<RecordGoodsReceiptCommand, GoodsReceiptDto>
{
    public async Task<GoodsReceiptDto> Handle(RecordGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0 || request.Lines.Select(line => line.PurchaseOrderLineId).Distinct().Count() != request.Lines.Count ||
            request.Lines.Any(line => line.QtyReceived <= 0 || line.QtyDamaged < 0 || line.QtyDamaged > line.QtyReceived || line.UnitCost < 0))
            throw new FluentValidation.ValidationException("Receipt lines must be unique with positive received, valid damaged quantities, and non-negative cost.");
        var order = await PurchaseOrderCommandHandler.LoadAsync(context, request.PurchaseOrderId, cancellationToken);
        if (order.Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
            throw new ConflictException("Only sent or partially received purchase orders can receive goods.");
        if (order.DeliverToLocationId != Guid.Empty && order.DeliverToLocationId != request.LocationId)
            throw new FluentValidation.ValidationException("Goods must be received at the purchase order delivery location.");
        if (!await context.Locations.AnyAsync(location => location.Id == request.LocationId && !location.IsDeleted, cancellationToken))
            throw new NotFoundException("Receipt location not found.");

        var orderLines = order.Lines.ToDictionary(line => line.Id);
        if (request.Lines.Any(line => !orderLines.ContainsKey(line.PurchaseOrderLineId)))
            throw new FluentValidation.ValidationException("Every receipt line must belong to the purchase order.");
        var productIds = request.Lines.Select(line => orderLines[line.PurchaseOrderLineId].ProductId).Distinct().ToList();
        var products = await context.Products.Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id, cancellationToken);
        if (products.Count != productIds.Count) throw new NotFoundException("A purchase-order product was not found.");
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        var requireExpiry = await context.Tenants.Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.RequireExpiryOnBatchReceipt).SingleAsync(cancellationToken);

        var receipt = new GoodsReceipt
        {
            PurchaseOrderId = order.Id, SupplierId = order.SupplierId, LocationId = request.LocationId,
            ReceiptNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            ReceivedBy = tenantContext.UserId, ReceivedAt = DateTime.UtcNow, Notes = request.Notes,
        };
        var movements = new List<StockMovementRequest>();
        foreach (var input in request.Lines)
        {
            var poLine = orderLines[input.PurchaseOrderLineId];
            var product = products[poLine.ProductId];
            Batch? batch = null;
            if (product.TrackingMode == TrackingMode.Batch)
            {
                if (string.IsNullOrWhiteSpace(input.BatchNumber)) throw new FluentValidation.ValidationException("Batch number is required for batch-tracked products.");
                if (requireExpiry && input.ExpiresAt is null) throw new FluentValidation.ValidationException("Expiry is required for this tenant's batch receipts.");
                if (input.ExpiresAt is not null && input.ExpiresAt.Value.Date <= DateTime.UtcNow.Date)
                    throw new FluentValidation.ValidationException("Batch expiry must be in the future.");
                batch = await context.Batches.SingleOrDefaultAsync(item => item.ProductId == poLine.ProductId &&
                    item.VariantId == poLine.VariantId && item.BatchNumber == input.BatchNumber.Trim(), cancellationToken);
                if (batch is null)
                {
                    batch = new Batch { ProductId = poLine.ProductId, VariantId = poLine.VariantId, SupplierId = order.SupplierId,
                        BatchNumber = input.BatchNumber.Trim(), ManufacturedAt = input.ManufacturedAt, ExpiresAt = input.ExpiresAt,
                        ReceivedAt = receipt.ReceivedAt, UnitCost = input.UnitCost };
                    context.Batches.Add(batch);
                }
            }
            var line = new GoodsReceiptLine { PurchaseOrderLineId = poLine.Id, ProductId = poLine.ProductId,
                VariantId = poLine.VariantId, BatchId = batch?.Id, QtyReceived = input.QtyReceived,
                QtyDamaged = input.QtyDamaged, UnitCost = input.UnitCost };
            receipt.Lines.Add(line);
            poLine.ReceivedQty += line.QtyAccepted;
            poLine.DamagedQty += line.QtyDamaged;
            if (line.QtyAccepted > 0)
                movements.Add(new StockMovementRequest(MovementType.Receipt, poLine.ProductId, request.LocationId,
                    line.QtyAccepted, poLine.VariantId, batch?.Id, input.UnitCost, "PurchaseOrderReceipt",
                    CorrelationId: receipt.Id));
        }
        context.GoodsReceipts.Add(receipt);
        await stockLedger.AppendAsync(movements, cancellationToken);
        SubmitPurchaseOrderCommandHandler.TryTransition(order.Lines.All(line => line.ReceivedQty >= line.OrderedQty)
            ? order.MarkFullyReceived : order.MarkPartiallyReceived);
        await context.SaveChangesAsync(cancellationToken);
        return new GoodsReceiptDto(receipt.Id, receipt.ReceiptNumber, order.Id, receipt.LocationId, receipt.ReceivedAt,
            order.Status, receipt.Lines.Select(line => new GoodsReceiptLineDto(line.Id, line.PurchaseOrderLineId,
                line.ProductId, line.VariantId, line.BatchId, line.QtyReceived, line.QtyDamaged, line.QtyAccepted, line.UnitCost)).ToList());
    }
}

public sealed class ClosePurchaseOrderShortCommandHandler(IAppDbContext context)
    : IRequestHandler<ClosePurchaseOrderShortCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(ClosePurchaseOrderShortCommand request, CancellationToken cancellationToken)
    {
        var order = await PurchaseOrderCommandHandler.LoadAsync(context, request.Id, cancellationToken);
        SubmitPurchaseOrderCommandHandler.TryTransition(() => order.CloseShort(request.Reason, DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken);
        return PurchaseOrderCommandHandler.Map(order);
    }
}
