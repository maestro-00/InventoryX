using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>
    /// Append-only ledger + StockLevel projection (research R5, T038). Stages
    /// entities on the shared DbContext; the caller's SaveChanges commits the
    /// movement, the projection update and its own entities in one transaction.
    /// </summary>
    public class StockLedger(AppDbContext context) : IStockLedger
    {
        public async Task AppendAsync(IReadOnlyList<StockMovementRequest> movements, CancellationToken cancellationToken = default)
        {
            foreach (var request in movements)
            {
                var level = await FindLevelAsync(request, cancellationToken);
                if (level is null)
                {
                    level = new StockLevel
                    {
                        ProductId = request.ProductId,
                        VariantId = request.VariantId,
                        LocationId = request.LocationId,
                        BatchId = request.BatchId,
                    };
                    context.StockLevels.Add(level);
                }

                var newQty = level.QtyOnHand + request.QtyDelta;
                if (newQty < 0 && !request.AllowNegative)
                    throw new ConflictException(
                        $"Insufficient stock: {level.QtyOnHand} on hand, movement of {request.QtyDelta} requested.");

                // Weighted-average cost on inbound stock (Cycle 1 valuation method)
                if (request.QtyDelta > 0 && request.UnitCost is not null)
                {
                    var existingValue = Math.Max(level.QtyOnHand, 0) * level.AvgUnitCost;
                    var incomingValue = request.QtyDelta * request.UnitCost.Value;
                    var divisor = Math.Max(level.QtyOnHand, 0) + request.QtyDelta;
                    level.AvgUnitCost = divisor <= 0 ? request.UnitCost.Value
                        : Math.Round((existingValue + incomingValue) / divisor, 4);
                }

                level.QtyOnHand = newQty;

                context.StockMovements.Add(new StockMovement
                {
                    Type = request.Type,
                    ProductId = request.ProductId,
                    VariantId = request.VariantId,
                    BatchId = request.BatchId,
                    LocationId = request.LocationId,
                    QtyDelta = request.QtyDelta,
                    UnitCost = request.UnitCost ?? level.AvgUnitCost,
                    ReasonCode = request.ReasonCode,
                    Note = request.Note,
                    CorrelationId = request.CorrelationId,
                    OccurredAt = request.OccurredAt ?? DateTime.UtcNow,
                });
            }
        }

        private async Task<StockLevel?> FindLevelAsync(StockMovementRequest request, CancellationToken cancellationToken)
        {
            // Prefer an entity already staged in this unit of work (e.g. two lines
            // of the same sale touching one product).
            var local = context.StockLevels.Local.FirstOrDefault(l =>
                l.ProductId == request.ProductId && l.VariantId == request.VariantId &&
                l.LocationId == request.LocationId && l.BatchId == request.BatchId);
            if (local is not null) return local;

            return await context.StockLevels.FirstOrDefaultAsync(l =>
                l.ProductId == request.ProductId && l.VariantId == request.VariantId &&
                l.LocationId == request.LocationId && l.BatchId == request.BatchId, cancellationToken);
        }
    }
}
