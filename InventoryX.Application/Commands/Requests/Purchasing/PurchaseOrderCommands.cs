using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Purchasing;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchasing;

public sealed record PurchaseOrderLineInput(Guid ProductId, Guid? VariantId, string Description, decimal OrderedQty, decimal UnitCost);
public sealed record PurchaseOrderLineDto(Guid Id, Guid ProductId, Guid? VariantId, string Description, decimal OrderedQty, decimal ReceivedQty, decimal DamagedQty, decimal UnitCost);
public sealed record PurchaseOrderDto(Guid Id, Guid SupplierId, PurchaseOrderStatus Status, PurchaseOrderOrigin Origin, Guid? OriginReferenceId, DateTime? RequiredBy, string? Notes, decimal Total, IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed class CreatePurchaseOrderCommand : IRequest<PurchaseOrderDto>, IFeatureGatedCommand
{
    public Guid SupplierId { get; init; }
    public PurchaseOrderOrigin Origin { get; init; }
    public Guid? OriginReferenceId { get; init; }
    public DateTime? RequiredBy { get; init; }
    public string? Notes { get; init; }
    public List<PurchaseOrderLineInput> Lines { get; init; } = [];
    public PlanFeature Feature => PlanFeature.PurchaseOrders;
}

public sealed class UpdatePurchaseOrderCommand : IRequest<PurchaseOrderDto>, ITenantWriteCommand
{
    public Guid Id { get; init; }
    public DateTime? RequiredBy { get; init; }
    public string? Notes { get; init; }
    public List<PurchaseOrderLineInput> Lines { get; init; } = [];
}

public sealed record SubmitPurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderDto>, ITenantWriteCommand;
public sealed record ApprovePurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderDto>, ITenantWriteCommand, IAuditedCommand
{ public string AuditAction => "purchase_order.approve"; public string AuditEntityType => "PurchaseOrder"; public string AuditEntityId => Id.ToString(); }
public sealed record RejectPurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderDto>, ITenantWriteCommand, IAuditedCommand
{ public string AuditAction => "purchase_order.reject"; public string AuditEntityType => "PurchaseOrder"; public string AuditEntityId => Id.ToString(); }
public sealed record CancelPurchaseOrderCommand(Guid Id, string Reason) : IRequest<PurchaseOrderDto>, ITenantWriteCommand, IAuditedCommand
{ public string AuditAction => "purchase_order.cancel"; public string AuditEntityType => "PurchaseOrder"; public string AuditEntityId => Id.ToString(); }
public sealed record SendPurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderEmailResult>, ITenantWriteCommand, IAuditedCommand
{ public string AuditAction => "purchase_order.email"; public string AuditEntityType => "PurchaseOrder"; public string AuditEntityId => Id.ToString(); }
