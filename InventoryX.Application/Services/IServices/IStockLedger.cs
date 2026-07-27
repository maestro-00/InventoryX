using InventoryX.Domain.Models.Inventory;

namespace InventoryX.Application.Services.IServices
{
    public record StockMovementRequest(
        MovementType Type,
        Guid ProductId,
        Guid LocationId,
        decimal QtyDelta,
        Guid? VariantId = null,
        Guid? BatchId = null,
        decimal? UnitCost = null,
        string? ReasonCode = null,
        string? Note = null,
        Guid? CorrelationId = null,
        /// <summary>Offline ingest may drive stock negative (conflict-flagged); online paths must not.</summary>
        bool AllowNegative = false,
        DateTime? OccurredAt = null);

    /// <summary>
    /// Append-only stock ledger (research R5): stages a StockMovement row and
    /// the matching StockLevel projection update on the current unit of work.
    /// The caller's SaveChanges commits both atomically with its own entities.
    /// </summary>
    public interface IStockLedger
    {
        Task AppendAsync(IReadOnlyList<StockMovementRequest> movements, CancellationToken cancellationToken = default);
    }
}
