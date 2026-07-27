using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory
{
    public class RecordStockAdjustmentCommandHandler(
        IAppDbContext context,
        IStockLedger stockLedger) : IRequestHandler<RecordStockAdjustmentCommand, RecordStockAdjustmentResult>
    {
        public async Task<RecordStockAdjustmentResult> Handle(RecordStockAdjustmentCommand request, CancellationToken cancellationToken)
        {
            if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId && !l.IsDeleted, cancellationToken))
                throw new NotFoundException("Location not found.");

            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
            var known = await context.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            var missing = productIds.Except(known).ToList();
            if (missing.Count > 0)
                throw new NotFoundException($"Product(s) not found: {string.Join(", ", missing)}");

            var correlationId = Guid.NewGuid();
            await stockLedger.AppendAsync(request.Lines.Select(line => new StockMovementRequest(
                MovementType.Adjustment,
                line.ProductId,
                request.LocationId,
                line.QtyDelta,
                VariantId: line.VariantId,
                UnitCost: line.UnitCost,
                ReasonCode: request.ReasonCode,
                Note: request.Note,
                CorrelationId: correlationId,
                AllowNegative: true)).ToList(), cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            return new RecordStockAdjustmentResult("Applied", productIds);
        }
    }
}
