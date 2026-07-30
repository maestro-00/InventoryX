using InventoryX.Application.Commands.Requests.Sync;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Sync;

public sealed class ResolveSyncConflictCommandHandler(
    IAppDbContext context,
    IStockLedger stockLedger,
    INotificationService notificationService) : IRequestHandler<ResolveSyncConflictCommand, SyncConflictResult>
{
    public async Task<SyncConflictResult> Handle(ResolveSyncConflictCommand request, CancellationToken cancellationToken)
    {
        var sale = await context.Sales.SingleOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
            ?? throw new NotFoundException("Conflicted sale not found.");
        if (!sale.StockConflictFlag) throw new ConflictException("The sale has no unresolved stock conflict.");
        var resolution = request.Resolution.Trim().ToLowerInvariant();
        if (resolution == "adjustwithreason")
        {
            if (string.IsNullOrWhiteSpace(request.ReasonCode) || request.Adjustments.Count == 0)
                throw new FluentValidation.ValidationException("Adjustment resolution requires a reason and at least one adjustment line.");
            await stockLedger.AppendAsync(request.Adjustments.Select(line => new StockMovementRequest(
                MovementType.Adjustment, line.ProductId, sale.LocationId, line.QtyDelta,
                VariantId: line.VariantId, UnitCost: line.UnitCost, ReasonCode: request.ReasonCode,
                Note: request.Note, CorrelationId: sale.Id, AllowNegative: true)).ToList(), cancellationToken);
        }
        else if (resolution != "acceptasis")
        {
            throw new FluentValidation.ValidationException("Resolution must be acceptAsIs or adjustWithReason.");
        }
        sale.StockConflictFlag = false;
        await context.SaveChangesAsync(cancellationToken);
        await notificationService.ResolveAsync(Key(sale.Id), cancellationToken);
        return new SyncConflictResult(sale.Id, request.Resolution, true);
    }

    public static string Key(Guid saleId) => $"stock-conflict:{saleId}";
}
