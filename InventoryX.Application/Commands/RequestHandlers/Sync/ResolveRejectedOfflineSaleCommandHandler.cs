using System.Text.Json;
using InventoryX.Application.Commands.Requests.Sync;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Sync;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Sync;

public sealed class ResolveRejectedOfflineSaleCommandHandler(
    IAppDbContext context,
    ITenantContext tenantContext) : IRequestHandler<ResolveRejectedOfflineSaleCommand, ResolveRejectedOfflineSaleResult>
{
    public async Task<ResolveRejectedOfflineSaleResult> Handle(
        ResolveRejectedOfflineSaleCommand request,
        CancellationToken cancellationToken)
    {
        var rejected = await context.RejectedOfflineSales
            .SingleOrDefaultAsync(r => r.Id == request.RejectedSaleId, cancellationToken)
            ?? throw new NotFoundException("Rejected offline sale not found.");

        if (rejected.Status != RejectedOfflineSaleStatus.Open)
            throw new ConflictException("The rejected sale has already been resolved.");

        var resolution = request.Resolution.Trim().ToLowerInvariant();
        if (resolution is "retryrelease" or "releaseretry")
        {
            rejected.Status = RejectedOfflineSaleStatus.ReleasedForRetry;
            rejected.ResolutionNote = request.Note;
            rejected.ResolvedBy = tenantContext.UserId;
            rejected.ResolvedAt = DateTime.UtcNow;
        }
        else if (resolution is "reconcilelinked" or "linkreconciliation")
        {
            if (request.LinkedReconciliationSaleId is null || request.LinkedReconciliationSaleId == Guid.Empty)
                throw new FluentValidation.ValidationException(
                    "Linked reconciliation requires LinkedReconciliationSaleId.");
            var linked = await context.Sales.AsNoTracking()
                .AnyAsync(s => s.Id == request.LinkedReconciliationSaleId, cancellationToken);
            if (!linked)
                throw new NotFoundException("Linked reconciliation sale not found.");
            rejected.Status = RejectedOfflineSaleStatus.Reconciled;
            rejected.LinkedReconciliationSaleId = request.LinkedReconciliationSaleId;
            rejected.ResolutionNote = request.Note;
            rejected.ResolvedBy = tenantContext.UserId;
            rejected.ResolvedAt = DateTime.UtcNow;
        }
        else
        {
            throw new FluentValidation.ValidationException(
                "Resolution must be retryRelease or reconcileLinked.");
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ResolveRejectedOfflineSaleResult(
            rejected.Id,
            rejected.ClientSaleId,
            rejected.Status.ToString(),
            rejected.LinkedReconciliationSaleId);
    }
}

public sealed class ListRejectedOfflineSalesQueryHandler(IAppDbContext context)
    : IRequestHandler<ListRejectedOfflineSalesQuery, List<RejectedOfflineSaleDto>>
{
    public async Task<List<RejectedOfflineSaleDto>> Handle(
        ListRejectedOfflineSalesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await context.RejectedOfflineSales.AsNoTracking()
            .Where(r => r.Status == RejectedOfflineSaleStatus.Open)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(r => new RejectedOfflineSaleDto(
            r.Id,
            r.ClientSaleId,
            r.RegisterId,
            r.RejectionReason,
            r.TraceId,
            r.Status.ToString(),
            r.LinkedReconciliationSaleId,
            r.PayloadHash)).ToList();
    }
}
