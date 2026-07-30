using InventoryX.Application.Behaviors;
using InventoryX.Application.Commands.Requests.Inventory;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Sync;

public record SyncConflictResult(Guid SaleId, string Resolution, bool Resolved);

public sealed class ResolveSyncConflictCommand : IRequest<SyncConflictResult>, ITenantWriteCommand, IAuditedCommand
{
    public Guid SaleId { get; init; }
    public string Resolution { get; init; } = "acceptAsIs";
    public string? ReasonCode { get; init; }
    public string? Note { get; init; }
    public List<AdjustmentLineDto> Adjustments { get; init; } = [];
    public string AuditAction => "sync.conflict.resolve";
    public string AuditEntityType => "Sale";
    public string AuditEntityId => SaleId.ToString();
}
