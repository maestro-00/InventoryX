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
        public async Task<IReadOnlyList<BatchAllocation>> AllocateFefoAsync(Guid productId, Guid? variantId,
            Guid locationId, decimal quantity, Guid? explicitBatchId = null, bool allowNegative = false,
            CancellationToken cancellationToken = default)
        {
            if (quantity <= 0) throw new FluentValidation.ValidationException("Issue quantity must be positive.");
            var today = DateTime.UtcNow.Date;
            var levels = await context.StockLevels.AsNoTracking()
                .Where(level => level.ProductId == productId && level.VariantId == variantId &&
                    level.LocationId == locationId && level.BatchId != null)
                .ToListAsync(cancellationToken);
            var batchIds = levels.Select(level => level.BatchId!.Value).Distinct().ToList();
            var batches = await context.Batches.AsNoTracking().Where(batch => batchIds.Contains(batch.Id))
                .ToDictionaryAsync(batch => batch.Id, cancellationToken);

            if (explicitBatchId is Guid requestedBatch)
            {
                if (!batches.TryGetValue(requestedBatch, out var batch) || batch.ProductId != productId ||
                    batch.VariantId != variantId || batch.ExpiresAt?.Date <= today)
                    throw new ConflictException("The requested batch is unavailable, expired, or does not belong to this product.");
                var available = levels.Single(level => level.BatchId == requestedBatch).QtyOnHand;
                if (available < quantity && !allowNegative)
                    throw new ConflictException($"Insufficient stock in batch {batch.BatchNumber}: {available} available, {quantity} requested.");
                return [new BatchAllocation(requestedBatch, quantity)];
            }

            var candidates = levels.Where(level => level.QtyOnHand > 0 && batches.TryGetValue(level.BatchId!.Value, out var batch) &&
                    (batch.ExpiresAt is null || batch.ExpiresAt.Value.Date > today))
                .OrderBy(level => batches[level.BatchId!.Value].ExpiresAt is null)
                .ThenBy(level => batches[level.BatchId!.Value].ExpiresAt)
                .ThenBy(level => batches[level.BatchId!.Value].ReceivedAt)
                .ThenBy(level => batches[level.BatchId!.Value].BatchNumber, StringComparer.Ordinal)
                .ToList();
            if (candidates.Count == 0) throw new ConflictException("No saleable batch stock is available for this product.");
            var remaining = quantity;
            var allocations = new List<BatchAllocation>();
            foreach (var level in candidates)
            {
                var allocated = Math.Min(level.QtyOnHand, remaining);
                if (allocated > 0) allocations.Add(new BatchAllocation(level.BatchId!.Value, allocated));
                remaining -= allocated;
                if (remaining <= 0) break;
            }
            if (remaining > 0)
            {
                if (!allowNegative) throw new ConflictException($"Insufficient batch stock: {quantity - remaining} available, {quantity} requested.");
                var first = allocations[0];
                allocations[0] = first with { Quantity = first.Quantity + remaining };
            }
            return allocations;
        }

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
