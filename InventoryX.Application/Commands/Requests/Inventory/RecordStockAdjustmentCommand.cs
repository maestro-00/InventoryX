using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Inventory
{
    public class AdjustmentLineDto
    {
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public decimal QtyDelta { get; init; }
        public decimal? UnitCost { get; init; }
    }

    public record RecordStockAdjustmentResult(string Status, List<Guid> MovementProductIds, Guid? AdjustmentId = null);

    /// <summary>
    /// Reasoned stock adjustment (T039 uses reason "Correction" for opening
    /// stock). US3 adds the above-threshold approval flow.
    /// </summary>
    public class RecordStockAdjustmentCommand : IRequest<RecordStockAdjustmentResult>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid LocationId { get; init; }
        public string ReasonCode { get; init; } = "Correction";
        public string? Note { get; init; }
        public List<AdjustmentLineDto> Lines { get; init; } = [];

        public string AuditAction => "stock.adjustment";
        public string AuditEntityType => "StockMovement";
        public string AuditEntityId => LocationId.ToString();
    }

    public sealed class ApproveStockAdjustmentCommand : IRequest<RecordStockAdjustmentResult>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid AdjustmentId { get; init; }
        public string AuditAction => "stock.adjustment.approve";
        public string AuditEntityType => "StockAdjustment";
        public string AuditEntityId => AdjustmentId.ToString();
    }

    public sealed class RejectStockAdjustmentCommand : IRequest<RecordStockAdjustmentResult>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid AdjustmentId { get; init; }
        public string AuditAction => "stock.adjustment.reject";
        public string AuditEntityType => "StockAdjustment";
        public string AuditEntityId => AdjustmentId.ToString();
    }
}
