using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory;

public sealed class RecordConsumptionCommandHandler(IAppDbContext context, IStockLedger stockLedger)
    : IRequestHandler<RecordConsumptionCommand, RecordStockAdjustmentResult>
{
    public async Task<RecordStockAdjustmentResult> Handle(RecordConsumptionCommand request, CancellationToken cancellationToken)
    {
        if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId && !l.IsDeleted, cancellationToken))
            throw new NotFoundException("Location not found.");
        if (request.Lines.Count == 0 || request.Lines.Any(line => line.QtyDelta <= 0))
            throw new FluentValidation.ValidationException("Consumption quantities must be greater than zero.");
        var productIds = request.Lines.Select(line => line.ProductId).Distinct().ToList();
        var known = await context.Products.Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id).ToListAsync(cancellationToken);
        if (productIds.Except(known).Any()) throw new NotFoundException("One or more products were not found.");

        var correlationId = Guid.NewGuid();
        await stockLedger.AppendAsync(request.Lines.Select(line => new StockMovementRequest(
            MovementType.Consumption, line.ProductId, request.LocationId, -line.QtyDelta,
            VariantId: line.VariantId, UnitCost: line.UnitCost, ReasonCode: request.ReasonCode,
            Note: request.Note, CorrelationId: correlationId)).ToList(), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new RecordStockAdjustmentResult("Applied", productIds, correlationId);
    }
}
