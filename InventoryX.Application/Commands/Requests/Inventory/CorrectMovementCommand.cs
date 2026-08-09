using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Inventory;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Inventory;

public sealed class CorrectMovementCommand : IRequest<StockMovementDto>, ITenantWriteCommand, IAuditedCommand
{
    public Guid MovementId { get; init; }
    public decimal CorrectedQtyDelta { get; init; }
    public string ReasonCode { get; init; } = "Correction";
    public string? Note { get; init; }
    public string AuditAction => "stock.movement.correct";
    public string AuditEntityType => "StockMovement";
    public string AuditEntityId => MovementId.ToString();
}
