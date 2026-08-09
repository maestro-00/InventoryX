using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory;

public class RecordStockAdjustmentCommandHandler(
    IAppDbContext context,
    IStockLedger stockLedger,
    ITenantContext? tenantContext = null) : IRequestHandler<RecordStockAdjustmentCommand, RecordStockAdjustmentResult>
{
    public async Task<RecordStockAdjustmentResult> Handle(RecordStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId && !l.IsDeleted, cancellationToken))
            throw new NotFoundException("Location not found.");
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await context.Products.Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var missing = productIds.Except(products.Keys).ToList();
        if (missing.Count > 0) throw new NotFoundException($"Product(s) not found: {string.Join(", ", missing)}");

        var tenant = tenantContext?.TenantId is Guid tenantId
            ? await context.Tenants.SingleAsync(t => t.Id == tenantId, cancellationToken)
            : await context.Tenants.SingleOrDefaultAsync(cancellationToken);
        var value = request.Lines.Sum(line => Math.Abs(line.QtyDelta) * (line.UnitCost ?? 0m));
        var needsApproval = tenant?.AdjustmentApprovalThreshold is decimal threshold && value > threshold;
        var adjustment = new StockAdjustment
        {
            LocationId = request.LocationId,
            ReasonCode = request.ReasonCode,
            Note = request.Note,
            RequestedBy = tenantContext?.UserId ?? "requester-1",
            Status = needsApproval ? StockAdjustmentStatus.AwaitingApproval : StockAdjustmentStatus.Applied,
            Lines = request.Lines.Select(line => new StockAdjustmentLine
            {
                ProductId = line.ProductId, VariantId = line.VariantId, QtyDelta = line.QtyDelta, UnitCost = line.UnitCost,
            }).ToList(),
        };
        context.StockAdjustments.Add(adjustment);
        if (!needsApproval)
            await PostAsync(adjustment, stockLedger, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new RecordStockAdjustmentResult(adjustment.Status.ToString(), needsApproval ? [] : productIds, adjustment.Id);
    }

    internal static Task PostAsync(StockAdjustment adjustment, IStockLedger stockLedger, CancellationToken cancellationToken) =>
        stockLedger.AppendAsync(adjustment.Lines.Select(line => new StockMovementRequest(
            MovementType.Adjustment, line.ProductId, adjustment.LocationId, line.QtyDelta,
            VariantId: line.VariantId, UnitCost: line.UnitCost, ReasonCode: adjustment.ReasonCode,
            Note: adjustment.Note, CorrelationId: adjustment.Id, AllowNegative: true)).ToList(), cancellationToken);
}

public sealed class ApproveStockAdjustmentCommandHandler(
    IAppDbContext context, IStockLedger stockLedger, ITenantContext tenantContext)
    : IRequestHandler<ApproveStockAdjustmentCommand, RecordStockAdjustmentResult>
{
    public async Task<RecordStockAdjustmentResult> Handle(ApproveStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await context.StockAdjustments.Include(a => a.Lines)
            .SingleOrDefaultAsync(a => a.Id == request.AdjustmentId, cancellationToken)
            ?? throw new NotFoundException("Stock adjustment not found.");
        if (adjustment.Status != StockAdjustmentStatus.AwaitingApproval)
            throw new ConflictException("Only awaiting adjustments can be approved.");
        var approver = tenantContext.UserId ?? "unknown";
        if (string.Equals(approver, adjustment.RequestedBy, StringComparison.Ordinal))
            throw new ConflictException("The adjustment approver must differ from the requester.");
        await RecordStockAdjustmentCommandHandler.PostAsync(adjustment, stockLedger, cancellationToken);
        adjustment.Status = StockAdjustmentStatus.Applied;
        adjustment.ApprovedBy = approver;
        await context.SaveChangesAsync(cancellationToken);
        return new RecordStockAdjustmentResult("Applied", adjustment.Lines.Select(l => l.ProductId).Distinct().ToList(), adjustment.Id);
    }
}

public sealed class RejectStockAdjustmentCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<RejectStockAdjustmentCommand, RecordStockAdjustmentResult>
{
    public async Task<RecordStockAdjustmentResult> Handle(RejectStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await context.StockAdjustments.Include(a => a.Lines)
            .SingleOrDefaultAsync(a => a.Id == request.AdjustmentId, cancellationToken)
            ?? throw new NotFoundException("Stock adjustment not found.");
        if (adjustment.Status != StockAdjustmentStatus.AwaitingApproval)
            throw new ConflictException("Only awaiting adjustments can be rejected.");
        adjustment.Status = StockAdjustmentStatus.Rejected;
        adjustment.ApprovedBy = tenantContext.UserId;
        await context.SaveChangesAsync(cancellationToken);
        return new RecordStockAdjustmentResult("Rejected", [], adjustment.Id);
    }
}
