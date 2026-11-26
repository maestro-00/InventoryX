using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Behaviors;

/// <summary>Replays completed owner mutations and rejects key/body mismatches.</summary>
public sealed class BillingIdempotencyBehavior<TRequest, TResponse>(
    IAppDbContext context, ITenantContext tenantContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IBillingIdempotentRequest idempotent)
            return await next(cancellationToken);
        var key = idempotent.IdempotencyKey;
        if (key is null) return await next(cancellationToken);
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200)
            throw new BillingIdempotencyConflictException("A valid Idempotency-Key header is required for this billing mutation.");
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("A tenant context is required for billing idempotency.");
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request, request.GetType()))));

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var previous = await context.BillingIdempotencyRecords.SingleOrDefaultAsync(
            record => record.Key == key, cancellationToken);
        if (previous is not null)
        {
            if (previous.RequestHash != requestHash)
                throw new BillingIdempotencyConflictException("The Idempotency-Key was already used with a different request.");
            return JsonSerializer.Deserialize<TResponse>(previous.ResponseJson)!;
        }

        try
        {
            var response = await next(cancellationToken);
            context.BillingIdempotencyRecords.Add(new BillingIdempotencyRecord
            {
                TenantId = tenantId,
                Key = key,
                RequestHash = requestHash,
                ResponseJson = JsonSerializer.Serialize(response),
                CompletedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            // A concurrent request may have committed the same key while this one ran.
            var winner = await context.BillingIdempotencyRecords.AsNoTracking()
                .SingleOrDefaultAsync(record => record.Key == key, cancellationToken);
            if (winner is null) throw;
            if (winner.RequestHash != requestHash)
                throw new BillingIdempotencyConflictException("The Idempotency-Key was already used with a different request.");
            return JsonSerializer.Deserialize<TResponse>(winner.ResponseJson)!;
        }
    }
}
