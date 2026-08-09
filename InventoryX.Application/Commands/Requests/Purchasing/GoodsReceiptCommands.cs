using InventoryX.Application.Behaviors;
using InventoryX.Domain.Models.Purchasing;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchasing;

public sealed record GoodsReceiptLineInput(Guid PurchaseOrderLineId, decimal QtyReceived, decimal QtyDamaged,
    decimal UnitCost, string? BatchNumber, DateTime? ExpiresAt, DateTime? ManufacturedAt = null);
public sealed record GoodsReceiptLineDto(Guid Id, Guid PurchaseOrderLineId, Guid ProductId, Guid? VariantId,
    Guid? BatchId, decimal QtyReceived, decimal QtyDamaged, decimal AcceptedQty, decimal UnitCost);
public sealed record GoodsReceiptDto(Guid Id, string ReceiptNumber, Guid PurchaseOrderId, Guid LocationId,
    DateTime ReceivedAt, PurchaseOrderStatus PurchaseOrderStatus, IReadOnlyList<GoodsReceiptLineDto> Lines);

public sealed class RecordGoodsReceiptCommand : IRequest<GoodsReceiptDto>, ITenantWriteCommand, IAuditedCommand
{
    public Guid PurchaseOrderId { get; init; }
    public Guid LocationId { get; init; }
    public string? Notes { get; init; }
    public List<GoodsReceiptLineInput> Lines { get; init; } = [];
    public string AuditAction => "goods_receipt.record";
    public string AuditEntityType => "PurchaseOrder";
    public string AuditEntityId => PurchaseOrderId.ToString();
}

public sealed record ClosePurchaseOrderShortCommand(Guid Id, string Reason) : IRequest<PurchaseOrderDto>, ITenantWriteCommand, IAuditedCommand
{ public string AuditAction => "purchase_order.close_short"; public string AuditEntityType => "PurchaseOrder"; public string AuditEntityId => Id.ToString(); }
