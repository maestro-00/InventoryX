using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services;

/// <summary>Commits a provider-verified charge, entitlement, and paid invoice together.</summary>
public sealed class BillingSettlementService(AppDbContext context, ITenantContext tenantContext) : IBillingSettlementService
{
    public async Task<bool> SettleAsync(
        string reference, decimal verifiedAmount, string verifiedCurrency,
        DateTime confirmedAt, CancellationToken cancellationToken = default)
    {
        if (tenantContext.TenantId is null)
            throw new InvalidOperationException("A tenant context is required to settle a billing payment.");
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("A payment reference is required.", nameof(reference));

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var attempt = await context.PaymentAttempts.SingleOrDefaultAsync(a => a.Reference == reference, cancellationToken);
        if (attempt is null) return false;
        var intent = await context.PaymentIntents.SingleAsync(i => i.Id == attempt.PaymentIntentId, cancellationToken);
        if (intent.Status == PaymentIntentStatus.Succeeded) return false;
        if (intent.BillingPeriodId is null)
            throw new InvalidOperationException("A payable intent requires a billing period.");
        var period = await context.BillingPeriods.SingleAsync(p => p.Id == intent.BillingPeriodId, cancellationToken);
        var subscription = await context.Subscriptions.SingleAsync(s => s.Id == intent.SubscriptionId, cancellationToken);
        if (await context.BillingInvoices.AnyAsync(i => i.PaymentIntentId == intent.Id, cancellationToken))
            throw new InvalidOperationException("An unsettled intent already has an invoice.");

        if (!SubscriptionStateMachine.SettleVerifiedPayment(
                subscription, period, intent, attempt, verifiedAmount, verifiedCurrency, confirmedAt))
            return false;

        context.BillingInvoices.Add(new BillingInvoice
        {
            TenantId = intent.TenantId,
            SubscriptionId = subscription.Id,
            BillingPeriodId = period.Id,
            PaymentIntentId = intent.Id,
            Number = $"BILL-{intent.Id:N}",
            Amount = intent.TotalAmount,
            TaxAmount = intent.TaxAmount,
            Currency = intent.Currency,
            Status = BillingInvoiceStatus.Paid,
            PaymentReference = attempt.Reference,
            IssuedAt = confirmedAt,
            DueAt = period.StartAt,
            PeriodStart = period.StartAt,
            PeriodEnd = period.EndAt,
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
