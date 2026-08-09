using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Inventory;

public sealed class RecordConsumptionCommand : IRequest<RecordStockAdjustmentResult>, ITenantWriteCommand, IAuditedCommand
{
    public Guid LocationId { get; init; }
    public string ReasonCode { get; init; } = "PersonalUse";
    public string? Note { get; init; }
    public List<AdjustmentLineDto> Lines { get; init; } = [];
    public string AuditAction => "stock.consumption";
    public string AuditEntityType => "StockMovement";
    public string AuditEntityId => LocationId.ToString();
}
