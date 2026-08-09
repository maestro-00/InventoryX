using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory;

public sealed class CreateStockTransferCommandHandler(IAppDbContext context) : IRequestHandler<CreateStockTransferCommand, StockTransferResult>
{
    public async Task<StockTransferResult> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
    {
        if (request.FromLocationId == request.ToLocationId || request.Lines.Count == 0 || request.Lines.Any(line => line.Quantity <= 0))
            throw new FluentValidation.ValidationException("Transfers need distinct locations and one or more positive lines.");
        var locations = await context.Locations.CountAsync(location =>
            (location.Id == request.FromLocationId || location.Id == request.ToLocationId) && !location.IsDeleted, cancellationToken);
        if (locations != 2) throw new NotFoundException("Transfer location not found.");
        var transfer = new StockTransfer
        {
            FromLocationId = request.FromLocationId,
            ToLocationId = request.ToLocationId,
            Lines = request.Lines.Select(line => new StockTransferLine
            {
                ProductId = line.ProductId, VariantId = line.VariantId, BatchId = line.BatchId, QtyDispatched = line.Quantity,
            }).ToList(),
        };
        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync(cancellationToken);
        return new StockTransferResult(transfer.Id, transfer.Status.ToString());
    }
}

public sealed class DispatchStockTransferCommandHandler(IAppDbContext context, IStockLedger ledger)
    : IRequestHandler<DispatchStockTransferCommand, StockTransferResult>
{
    public async Task<StockTransferResult> Handle(DispatchStockTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await StockTransferLookup.GetTransfer(context, request.TransferId, cancellationToken);
        if (transfer.Status != StockTransferStatus.Draft)
            throw new ConflictException("Only a draft transfer can be dispatched.");
        await ledger.AppendAsync(transfer.Lines.Select(line => new StockMovementRequest(
            MovementType.TransferOut, line.ProductId, transfer.FromLocationId, -line.QtyDispatched,
            line.VariantId, line.BatchId, CorrelationId: transfer.Id)).ToList(), cancellationToken);
        foreach (var line in transfer.Lines)
        {
            var sourceLevel = await context.StockLevels.SingleAsync(level => level.ProductId == line.ProductId &&
                level.VariantId == line.VariantId && level.BatchId == line.BatchId && level.LocationId == transfer.FromLocationId, cancellationToken);
            sourceLevel.QtyInTransit += line.QtyDispatched;
        }
        transfer.Status = StockTransferStatus.Dispatched;
        await context.SaveChangesAsync(cancellationToken);
        return new StockTransferResult(transfer.Id, transfer.Status.ToString());
    }
}

public sealed class ReceiveStockTransferCommandHandler(IAppDbContext context, IStockLedger ledger)
    : IRequestHandler<ReceiveStockTransferCommand, StockTransferResult>
{
    public async Task<StockTransferResult> Handle(ReceiveStockTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await StockTransferLookup.GetTransfer(context, request.TransferId, cancellationToken);
        if (transfer.Status != StockTransferStatus.Dispatched)
            throw new ConflictException("Only a dispatched transfer can be received.");
        var received = request.Lines.ToDictionary(line => line.LineId, line => line.QuantityReceived);
        if (received.Count != transfer.Lines.Count || transfer.Lines.Any(line => !received.TryGetValue(line.Id, out var qty) || qty < 0 || qty > line.QtyDispatched))
            throw new FluentValidation.ValidationException("Every line must be received with a quantity between zero and the dispatched amount.");
        var hasDiscrepancy = transfer.Lines.Any(line => received[line.Id] != line.QtyDispatched);
        if (hasDiscrepancy && string.IsNullOrWhiteSpace(request.DiscrepancyReason))
            throw new FluentValidation.ValidationException("A discrepancy reason is required when received quantities differ.");
        await ledger.AppendAsync(transfer.Lines.Select(line => new StockMovementRequest(
            MovementType.TransferIn, line.ProductId, transfer.ToLocationId, received[line.Id],
            line.VariantId, line.BatchId, CorrelationId: transfer.Id, AllowNegative: true)).ToList(), cancellationToken);
        foreach (var line in transfer.Lines)
        {
            line.QtyReceived = received[line.Id];
            var sourceLevel = await context.StockLevels.SingleAsync(level => level.ProductId == line.ProductId &&
                level.VariantId == line.VariantId && level.BatchId == line.BatchId && level.LocationId == transfer.FromLocationId, cancellationToken);
            sourceLevel.QtyInTransit -= line.QtyDispatched;
        }
        transfer.DiscrepancyReason = hasDiscrepancy ? request.DiscrepancyReason : null;
        transfer.Status = hasDiscrepancy ? StockTransferStatus.ReceivedWithDiscrepancy : StockTransferStatus.Received;
        await context.SaveChangesAsync(cancellationToken);
        return new StockTransferResult(transfer.Id, transfer.Status.ToString(), transfer.DiscrepancyReason);
    }
}

file static class StockTransferLookup
{
    public static async Task<StockTransfer> GetTransfer(IAppDbContext context, Guid id, CancellationToken cancellationToken) =>
        await context.StockTransfers.Include(transfer => transfer.Lines).SingleOrDefaultAsync(transfer => transfer.Id == id, cancellationToken)
        ?? throw new NotFoundException("Stock transfer not found.");
}
