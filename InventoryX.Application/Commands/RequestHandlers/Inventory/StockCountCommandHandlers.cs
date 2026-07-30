using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory;

internal static class StockCountMapping
{
    public static StockCountResult ToResult(StockCount count) => new(count.Id, count.Scope.ToString(), count.Status.ToString(), count.LocationId,
        count.Lines.Select(l => new StockCountLineResult(l.Id, l.ProductId, l.ExpectedQty, l.CountedQty, l.VarianceQty, l.VarianceValue)).ToList());
}

public sealed class OpenStockCountCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<OpenStockCountCommand, StockCountResult>
{
    public async Task<StockCountResult> Handle(OpenStockCountCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<StockCountScope>(request.Scope, true, out var scope))
            throw new FluentValidation.ValidationException("Unknown stock-count scope.");
        if (scope == StockCountScope.Spot && request.ProductIds.Count == 0 && request.CategoryId is null)
            throw new FluentValidation.ValidationException("Spot counts require productIds or categoryId.");
        if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId && !l.IsDeleted, cancellationToken))
            throw new NotFoundException("Location not found.");

        var levels = context.StockLevels.Where(l => l.LocationId == request.LocationId);
        if (request.ProductIds.Count > 0) levels = levels.Where(l => request.ProductIds.Contains(l.ProductId));
        if (request.CategoryId is Guid categoryId)
        {
            var ids = context.Products.Where(p => p.CategoryId == categoryId).Select(p => p.Id);
            levels = levels.Where(l => ids.Contains(l.ProductId));
        }
        var snapshot = await levels.ToListAsync(cancellationToken);
        var count = new StockCount
        {
            LocationId = request.LocationId, Scope = scope, Status = StockCountStatus.Open,
            CountedBy = tenantContext.UserId, OpenedAt = DateTime.UtcNow,
            Lines = snapshot.Select(level => new StockCountLine
            {
                ProductId = level.ProductId, VariantId = level.VariantId, BatchId = level.BatchId,
                ExpectedQty = level.QtyOnHand, UnitCost = level.AvgUnitCost,
            }).ToList(),
        };
        context.StockCounts.Add(count);
        await context.SaveChangesAsync(cancellationToken);
        return StockCountMapping.ToResult(count);
    }
}

public sealed class UpdateStockCountLinesCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateStockCountLinesCommand, StockCountResult>
{
    public async Task<StockCountResult> Handle(UpdateStockCountLinesCommand request, CancellationToken cancellationToken)
    {
        var count = await LoadAsync(context, request.CountId, cancellationToken);
        if (count.Status is not (StockCountStatus.Open or StockCountStatus.Counting))
            throw new ConflictException("Only open counts accept quantities.");
        foreach (var input in request.Lines)
        {
            if (input.CountedQty < 0) throw new FluentValidation.ValidationException("Counted quantity cannot be negative.");
            var line = count.Lines.SingleOrDefault(l => l.Id == input.LineId) ?? throw new NotFoundException("Stock-count line not found.");
            line.CountedQty = input.CountedQty;
        }
        count.Status = StockCountStatus.Counting;
        await context.SaveChangesAsync(cancellationToken);
        return StockCountMapping.ToResult(count);
    }

    internal static async Task<StockCount> LoadAsync(IAppDbContext context, Guid id, CancellationToken ct) =>
        await context.StockCounts.Include(c => c.Lines).SingleOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NotFoundException("Stock count not found.");
}

public sealed class SubmitStockCountCommandHandler(IAppDbContext context) : IRequestHandler<SubmitStockCountCommand, StockCountResult>
{
    public async Task<StockCountResult> Handle(SubmitStockCountCommand request, CancellationToken cancellationToken)
    {
        var count = await UpdateStockCountLinesCommandHandler.LoadAsync(context, request.CountId, cancellationToken);
        if (count.Status is not (StockCountStatus.Open or StockCountStatus.Counting))
            throw new ConflictException("Only open counts can be submitted.");
        if (count.Lines.Any(l => l.CountedQty is null))
            throw new FluentValidation.ValidationException("Every count line requires a counted quantity.");
        foreach (var line in count.Lines)
        {
            line.VarianceQty = line.CountedQty!.Value - line.ExpectedQty;
            line.VarianceValue = Math.Round(line.VarianceQty * line.UnitCost, 4);
        }
        count.Status = StockCountStatus.AwaitingApproval;
        count.SubmittedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return StockCountMapping.ToResult(count);
    }
}

public sealed class ApproveStockCountCommandHandler(IAppDbContext context, IStockLedger stockLedger, ITenantContext tenantContext)
    : IRequestHandler<ApproveStockCountCommand, StockCountResult>
{
    public async Task<StockCountResult> Handle(ApproveStockCountCommand request, CancellationToken cancellationToken)
    {
        var count = await UpdateStockCountLinesCommandHandler.LoadAsync(context, request.CountId, cancellationToken);
        if (count.Status != StockCountStatus.AwaitingApproval) throw new ConflictException("Only submitted counts can be approved.");
        var movements = count.Lines.Where(l => l.VarianceQty != 0).Select(l => new StockMovementRequest(
            MovementType.CountCorrection, l.ProductId, count.LocationId, l.VarianceQty,
            VariantId: l.VariantId, BatchId: l.BatchId, UnitCost: l.UnitCost,
            ReasonCode: "StockCount", CorrelationId: count.Id, AllowNegative: true)).ToList();
        if (movements.Count > 0) await stockLedger.AppendAsync(movements, cancellationToken);
        count.Status = StockCountStatus.Approved;
        count.ApprovedBy = tenantContext.UserId;
        count.ApprovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return StockCountMapping.ToResult(count);
    }
}

public sealed class RejectStockCountCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<RejectStockCountCommand, StockCountResult>
{
    public async Task<StockCountResult> Handle(RejectStockCountCommand request, CancellationToken cancellationToken)
    {
        var count = await UpdateStockCountLinesCommandHandler.LoadAsync(context, request.CountId, cancellationToken);
        if (count.Status != StockCountStatus.AwaitingApproval) throw new ConflictException("Only submitted counts can be rejected.");
        count.Status = StockCountStatus.Rejected;
        count.ApprovedBy = tenantContext.UserId;
        count.ApprovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return StockCountMapping.ToResult(count);
    }
}
