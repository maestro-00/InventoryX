using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory;

public sealed class CorrectMovementCommandHandler(IAppDbContext context, IStockLedger stockLedger)
    : IRequestHandler<CorrectMovementCommand, StockMovementDto>
{
    public async Task<StockMovementDto> Handle(CorrectMovementCommand request, CancellationToken cancellationToken)
    {
        var original = await context.StockMovements.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == request.MovementId, cancellationToken)
            ?? throw new NotFoundException("Stock movement not found.");
        var delta = request.CorrectedQtyDelta - original.QtyDelta;
        if (delta == 0) throw new FluentValidation.ValidationException("Corrected quantity must differ from the original movement.");

        await stockLedger.AppendAsync([new StockMovementRequest(
            MovementType.Adjustment, original.ProductId, original.LocationId, delta,
            VariantId: original.VariantId, BatchId: original.BatchId, UnitCost: original.UnitCost,
            ReasonCode: request.ReasonCode, Note: request.Note, CorrelationId: original.Id,
            AllowNegative: true)], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        var correction = await context.StockMovements.AsNoTracking()
            .SingleAsync(m => m.CorrelationId == original.Id && m.Id != original.Id, cancellationToken);
        return new StockMovementDto
        {
            Id = correction.Id, Type = correction.Type.ToString(), ProductId = correction.ProductId,
            VariantId = correction.VariantId, BatchId = correction.BatchId, LocationId = correction.LocationId,
            QtyDelta = correction.QtyDelta, ReasonCode = correction.ReasonCode, Note = correction.Note,
            UserId = correction.UserId, OccurredAt = correction.OccurredAt,
        };
    }
}
