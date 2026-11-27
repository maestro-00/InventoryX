using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryX.Infrastructure.Services;

/// <summary>Commits a provider-verified charge, entitlement, and paid invoice together.</summary>
public sealed class BillingSettlementService(AppDbContext context, ITenantContext tenantContext,
    IBillingSecretProtector? secretProtector = null) : IBillingSettlementService
{
    public async Task<bool> SettleAsync(
        string reference, decimal verifiedAmount, string verifiedCurrency,
        DateTime confirmedAt, CancellationToken cancellationToken = default,
        string? authorizationCode = null, string? cardBrand = null,
        string? cardLast4 = null, int? cardExpiryMonth = null, int? cardExpiryYear = null)
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
        if (intent.Purpose == PaymentIntentPurpose.MethodVerification)
        {
            if (verifiedAmount != attempt.Amount || verifiedAmount != intent.TotalAmount ||
                !string.Equals(verifiedCurrency, attempt.Currency, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(verifiedCurrency, intent.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Verified method payment does not match the billing snapshot.");
            if (attempt.PaymentMethodId is null)
                throw new InvalidOperationException("A replacement attempt requires a pending method.");
            var method = await context.PaymentMethods.SingleAsync(item => item.Id == attempt.PaymentMethodId,
                cancellationToken);
            var replacementSubscription = await context.Subscriptions.SingleAsync(item => item.Id == intent.SubscriptionId,
                cancellationToken);
            if (method.TenantId != intent.TenantId || method.Status != PaymentMethodStatus.Pending)
                throw new InvalidOperationException("The replacement method is not pending for this tenant.");
            if (await context.BillingInvoices.AnyAsync(item => item.PaymentIntentId == intent.Id,
                cancellationToken))
                throw new InvalidOperationException("The method verification already has an invoice.");
            var canActivate = method.Kind == PaymentMethodKind.MobileMoney ||
                !string.IsNullOrWhiteSpace(authorizationCode);
            if (canActivate)
            {
                if (replacementSubscription.CurrentPaymentMethodId is { } previousId)
                {
                    var previous = await context.PaymentMethods.SingleAsync(item => item.Id == previousId,
                        cancellationToken);
                    previous.Retire(confirmedAt);
                    await context.SaveChangesAsync(cancellationToken);
                }
                if (method.Kind == PaymentMethodKind.Card)
                {
                    method.EncryptedAuthorizationCode = (secretProtector ?? throw new InvalidOperationException(
                        "Billing secret protection is required.")).Protect(intent.TenantId, authorizationCode!);
                    method.Brand = cardBrand;
                    method.MaskedIdentifier = cardLast4 is { Length: 4 } ? $"**** {cardLast4}" : "Card";
                    method.ExpiryMonth = cardExpiryMonth;
                    method.ExpiryYear = cardExpiryYear;
                }
                method.Activate(confirmedAt);
                replacementSubscription.CurrentPaymentMethodId = method.Id;
            }
            else method.Invalidate();
            attempt.MarkSucceeded(confirmedAt);
            intent.MarkSucceeded(confirmedAt);
            var replacementTenant = await context.Tenants.SingleAsync(item => item.Id == intent.TenantId,
                cancellationToken);
            var replacementInvoice = new BillingInvoice
            {
                TenantId = intent.TenantId, SubscriptionId = replacementSubscription.Id,
                PaymentIntentId = intent.Id, Number = $"BILL-{intent.Id:N}",
                Amount = intent.TotalAmount, TaxAmount = intent.TaxAmount, Currency = intent.Currency,
                Status = BillingInvoiceStatus.Paid, PaymentReference = attempt.Reference,
                IssuedAt = confirmedAt, DueAt = confirmedAt,
                BillingContactSnapshot = replacementTenant.BillingEmail,
                TaxDetailsSnapshot = replacementTenant.BillingTaxNumber,
            };
            replacementInvoice.PdfContent = Convert.ToBase64String(InvoicePdfService.RenderInvoice(replacementInvoice,
                replacementTenant.Name, "Payment method verification"));
            context.BillingInvoices.Add(replacementInvoice);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        if (intent.BillingPeriodId is null)
            throw new InvalidOperationException("A payable intent requires a billing period.");
        var period = await context.BillingPeriods.SingleAsync(p => p.Id == intent.BillingPeriodId, cancellationToken);
        var subscription = await context.Subscriptions.SingleAsync(s => s.Id == intent.SubscriptionId, cancellationToken);
        if (await context.BillingInvoices.AnyAsync(i => i.PaymentIntentId == intent.Id, cancellationToken))
            throw new InvalidOperationException("An unsettled intent already has an invoice.");

        if (!SubscriptionStateMachine.SettleVerifiedPayment(
                subscription, period, intent, attempt, verifiedAmount, verifiedCurrency, confirmedAt))
            return false;

        if (intent.Purpose == PaymentIntentPurpose.Initial && attempt.PaymentMethodId is { } methodId)
        {
            var method = await context.PaymentMethods.SingleAsync(item => item.Id == methodId, cancellationToken);
            if (method.Kind == PaymentMethodKind.Card && !string.IsNullOrWhiteSpace(authorizationCode))
            {
                method.EncryptedAuthorizationCode = (secretProtector ?? throw new InvalidOperationException(
                    "Billing secret protection is required.")).Protect(intent.TenantId, authorizationCode);
                method.Brand = cardBrand;
                method.MaskedIdentifier = cardLast4 is { Length: 4 } ? $"**** {cardLast4}" : "Card";
                method.ExpiryMonth = cardExpiryMonth;
                method.ExpiryYear = cardExpiryYear;
                method.Activate(confirmedAt);
            }
            else if (method.Kind == PaymentMethodKind.Card)
                method.Invalidate();
            else if (method.Kind == PaymentMethodKind.MobileMoney)
                method.Activate(confirmedAt);
            if (method.Status == PaymentMethodStatus.Active)
            {
                if (subscription.CurrentPaymentMethodId is { } previousId && previousId != method.Id)
                {
                    var previous = await context.PaymentMethods.SingleAsync(item => item.Id == previousId, cancellationToken);
                    previous.Retire(confirmedAt);
                }
                subscription.CurrentPaymentMethodId = method.Id;
            }
        }

        var tenant = await context.Tenants.SingleAsync(item => item.Id == intent.TenantId, cancellationToken);
        var plan = await context.PlanDefinitions.SingleOrDefaultAsync(item => item.Id == period.PlanDefinitionId,
            cancellationToken);
        var invoice = new BillingInvoice
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
            BillingContactSnapshot = tenant.BillingEmail,
            TaxDetailsSnapshot = tenant.BillingTaxNumber,
        };
        invoice.PdfContent = Convert.ToBase64String(InvoicePdfService.RenderInvoice(invoice,
            tenant.Name, plan?.Name ?? "Subscription"));
        context.BillingInvoices.Add(invoice);
        context.AuditLogEntries.Add(new AuditLogEntry
        {
            TenantId = intent.TenantId,
            Actor = tenantContext.UserId ?? "system",
            Action = "billing.payment.settled",
            EntityType = "PaymentIntent",
            EntityId = intent.Id.ToString(),
            OccurredAt = confirmedAt,
            AfterJson = JsonSerializer.Serialize(new
            {
                intent.Purpose, intent.Status, amount = intent.TotalAmount,
                intent.Currency, outcome = "confirmed", period.StartAt, period.EndAt,
            }),
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
