using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

/// <summary>Drives subscription time transitions and records retry/purge work in the durable outbox.</summary>
public sealed class BillingWorker(IServiceScopeFactory scopeFactory, ILogger<BillingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception exception) { logger.LogError(exception, "Billing worker iteration failed."); }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var protector = scope.ServiceProvider.GetRequiredService<IBillingSecretProtector>();
        var settlement = scope.ServiceProvider.GetRequiredService<IBillingSettlementService>();
        var notifications = scope.ServiceProvider.GetService<INotificationService>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var now = DateTime.UtcNow;
        var subscriptions = await context.Subscriptions.IgnoreQueryFilters().Include(item => item.Plan).ToListAsync(cancellationToken);
        var freePlan = await context.PlanDefinitions.FirstOrDefaultAsync(item => item.Tier == PlanTier.Free, cancellationToken);

        foreach (var subscription in subscriptions)
        {
            tenantContext.TenantId = subscription.TenantId;
            var scheduledChange = await context.ScheduledSubscriptionChanges.IgnoreQueryFilters()
                .SingleOrDefaultAsync(change => change.SubscriptionId == subscription.Id &&
                    change.Status == ScheduledChangeStatus.Scheduled, cancellationToken);
            if (scheduledChange is not null && scheduledChange.EffectiveAt <= now)
            {
                if (scheduledChange.Kind == ScheduledChangeKind.Cancel)
                {
                    scheduledChange.Apply();
                    subscription.Status = SubscriptionStatus.Cancelled;
                    SubscriptionStateMachine.StartPurgeClock(subscription, now);
                    continue;
                }
                if (scheduledChange.TargetPlanDefinitionId is { } targetPlanId)
                {
                    subscription.PlanDefinitionId = targetPlanId;
                    subscription.PendingPlanDefinitionId = null;
                    subscription.Plan = await context.PlanDefinitions.SingleAsync(plan => plan.Id == targetPlanId, cancellationToken);
                }
                if (scheduledChange.TargetCycle is { } targetCycle)
                    subscription.BillingCycle = targetCycle;
                scheduledChange.Apply();
            }
            if (subscription.Status == SubscriptionStatus.Trialing && freePlan is not null)
                SubscriptionStateMachine.ExpireTrialToFree(subscription, freePlan.Id, now);
            if (subscription.Status == SubscriptionStatus.PastDue)
            {
                if (subscription.GraceExpiresAt <= now)
                {
                    SubscriptionStateMachine.ExhaustGrace(subscription, now);
                    BillingMetrics.GraceLocks.Add(1, new KeyValuePair<string, object?>("state", "readonly"));
                }
                else if (subscription.LastChargeAttemptAt is null || subscription.LastChargeAttemptAt <= now.AddDays(-1))
                    await RetryChargeAsync(context, gateway, protector, settlement, subscription, now, cancellationToken);
                if (notifications is not null)
                    await notifications.RaiseAsync(NotificationType.BillingFailure,
                        $"billing:failure:{subscription.Id:N}:{subscription.GraceExpiresAt:O}",
                        "Payment recovery required",
                        $"An outstanding billing balance is due before {subscription.GraceExpiresAt:O}. Recover billing at /settings/billing.",
                        cancellationToken: cancellationToken);
            }
            if (subscription.Status == SubscriptionStatus.PastDue && subscription.GraceExpiresAt is not null &&
                subscription.GraceExpiresAt <= now.AddDays(1))
            {
                var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(tenant.BillingEmail))
                    context.OutboxMessages.Add(EmailOutbox.Html(subscription.TenantId,
                        $"billing:lock-warning:{subscription.Id:N}:{subscription.GraceExpiresAt:O}", tenant.BillingEmail,
                        "Billing grace period ending", $"Your billing grace period ends at {subscription.GraceExpiresAt:O}. Recover billing at /settings/billing.", now));
                if (notifications is not null)
                    await notifications.RaiseAsync(NotificationType.BillingFailure,
                        $"billing:lock-warning:{subscription.Id:N}:{subscription.GraceExpiresAt:O}",
                        "Billing grace period ending", $"Paid actions may lock at {subscription.GraceExpiresAt:O}; recover billing at /settings/billing.", cancellationToken: cancellationToken);
            }
            if (subscription.CancelledAt is not null && subscription.PurgeAt is not null &&
                subscription.RetentionWarningAcceptedAt is null && subscription.PurgeAt <= now.AddDays(7))
            {
                var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(tenant.BillingEmail))
                    context.OutboxMessages.Add(EmailOutbox.Html(subscription.TenantId,
                        $"billing:retention-warning:{subscription.Id:N}:{subscription.PurgeAt:O}", tenant.BillingEmail,
                        "Your InventoryX data retention deadline", $"Export your data before the UTC deletion deadline {subscription.PurgeAt:O}: /settings/billing.", now));
            }
            else if (subscription.Status == SubscriptionStatus.Active && subscription.CancelledAt is null && subscription.CurrentPeriodEnd <= now)
                await RetryChargeAsync(context, gateway, protector, settlement, subscription, now, cancellationToken);
            else if (subscription.Status == SubscriptionStatus.Active && subscription.CancelledAt is null &&
                subscription.CurrentPeriodEnd > now && subscription.CurrentPeriodEnd <= now.AddDays(1) &&
                subscription.CurrentPaymentMethodId is not null &&
                await context.PaymentMethods.IgnoreQueryFilters().AnyAsync(method =>
                    method.Id == subscription.CurrentPaymentMethodId &&
                    method.Kind == PaymentMethodKind.MobileMoney && method.Status == PaymentMethodStatus.Active,
                    cancellationToken))
                await RetryChargeAsync(context, gateway, protector, settlement, subscription, now, cancellationToken);
            else if (subscription.CancelledAt <= now && subscription.Status != SubscriptionStatus.PurgePending)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                SubscriptionStateMachine.StartPurgeClock(subscription, now);
            }

            if (subscription.Status == SubscriptionStatus.PurgePending && subscription.PurgeAt <= now)
            {
                var acceptedDeadline = subscription.RetentionWarningAcceptedAt?.AddDays(7);
                if (acceptedDeadline is null || acceptedDeadline > now || subscription.RetentionWarningAcceptedAt is null)
                {
                    subscription.PurgeAt = acceptedDeadline ?? now.AddDays(7);
                    BillingMetrics.RetentionPostponements.Add(1);
                    Enqueue(context, subscription.TenantId, "billing.retention.deadline-corrected",
                        $"{{\"tenantId\":\"{subscription.TenantId}\",\"purgeAt\":\"{subscription.PurgeAt:O}\"}}");
                }
                else
                    Enqueue(context, subscription.TenantId, "tenant.purge.requested", $"{{\"tenantId\":\"{subscription.TenantId}\"}}");
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task RetryChargeAsync(AppDbContext context, IPaymentGateway gateway,
        IBillingSecretProtector protector, IBillingSettlementService settlement,
        Subscription subscription, DateTime now, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
        var method = subscription.CurrentPaymentMethodId is { } methodId
            ? await context.PaymentMethods.IgnoreQueryFilters().SingleOrDefaultAsync(item =>
                item.Id == methodId && item.TenantId == subscription.TenantId &&
                item.Status == PaymentMethodStatus.Active, cancellationToken) : null;
        if (method is null || string.IsNullOrWhiteSpace(tenant.BillingEmail))
        {
            if (subscription.CurrentPeriodEnd <= now)
                SubscriptionStateMachine.RecordChargeFailure(subscription, subscription.CurrentPeriodEnd);
            return;
        }

        var anchor = subscription.BillingAnchor == default
            ? subscription.CurrentPeriodEnd : subscription.BillingAnchor;
        var (startOffset, endOffset) = BillingPeriodService.Containing(
            new DateTimeOffset(DateTime.SpecifyKind(anchor, DateTimeKind.Utc)), subscription.BillingCycle,
            new DateTimeOffset(now < subscription.CurrentPeriodEnd ? subscription.CurrentPeriodEnd : now));
        var start = startOffset.UtcDateTime;
        var end = endOffset.UtcDateTime;
        var period = await context.BillingPeriods.IgnoreQueryFilters().SingleOrDefaultAsync(item =>
            item.SubscriptionId == subscription.Id && item.StartAt == start &&
            item.Cycle == subscription.BillingCycle, cancellationToken);
        if (period is null)
        {
            var amount = subscription.BillingCycle == BillingCycle.Annual
                ? subscription.Plan!.AnnualPrice : subscription.Plan!.MonthlyPrice;
            period = new BillingPeriod
            {
                TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
                StartAt = start, EndAt = end, Cycle = subscription.BillingCycle,
                PlanDefinitionId = subscription.PlanDefinitionId,
                BaseAmount = amount, TotalAmount = amount, Currency = tenant.Currency,
                PriceVersion = $"renewal:{subscription.PlanDefinitionId:N}:{subscription.BillingCycle}:{amount}",
            };
            context.BillingPeriods.Add(period);
        }
        if (period.Status is BillingPeriodStatus.Paid or BillingPeriodStatus.Waived) return;
        var intent = await context.PaymentIntents.IgnoreQueryFilters().SingleOrDefaultAsync(item =>
            item.BillingPeriodId == period.Id && item.Purpose == PaymentIntentPurpose.Renewal,
            cancellationToken);
        if (intent is null)
        {
            intent = new PaymentIntent
            {
                TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
                BillingPeriodId = period.Id, Purpose = PaymentIntentPurpose.Renewal,
                BaseAmount = period.BaseAmount, TaxAmount = period.TaxAmount,
                TotalAmount = period.TotalAmount, Currency = period.Currency,
                ExpectedPeriodStart = start, ExpectedPeriodEnd = end,
                IdempotencyKey = $"renewal:{subscription.Id:N}:{start:O}",
            };
            context.PaymentIntents.Add(intent);
        }
        if (intent.Status == PaymentIntentStatus.Succeeded) return;
        var attempt = await context.PaymentAttempts.IgnoreQueryFilters()
            .Where(item => item.PaymentIntentId == intent.Id)
            .OrderByDescending(item => item.InitiatedAt).FirstOrDefaultAsync(cancellationToken);
        if (attempt is not null && attempt.Status is PaymentAttemptStatus.Created or
            PaymentAttemptStatus.Unknown or PaymentAttemptStatus.AwaitingApproval)
        {
            await VerifyAttemptAsync(context, gateway, settlement, subscription, period, intent, attempt,
                now, cancellationToken);
            if (method.Kind == PaymentMethodKind.MobileMoney &&
                attempt.Status == PaymentAttemptStatus.AwaitingApproval && start <= now &&
                subscription.Status == SubscriptionStatus.Active)
            {
                period.MarkPastDue();
                SubscriptionStateMachine.RecordChargeFailure(subscription, start);
            }
            return;
        }
        attempt = new PaymentAttempt
        {
            TenantId = subscription.TenantId, PaymentIntentId = intent.Id,
            PaymentMethodId = method.Id, Reference = $"renewal-{Guid.NewGuid():N}",
            Amount = period.TotalAmount, Currency = period.Currency, InitiatedAt = now,
        };
        context.PaymentAttempts.Add(attempt);
        BillingMetrics.RenewalAttempts.Add(1,
            new KeyValuePair<string, object?>("method", method.Kind.ToString()));
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var charge = await gateway.ChargeAsync(new PaymentChargeRequest(
                tenant.BillingEmail, period.TotalAmount, period.Currency,
                AuthorizationCode: method.Kind == PaymentMethodKind.Card
                    ? protector.Unprotect(subscription.TenantId, method.EncryptedAuthorizationCode!) : null,
                MobileMoneyProvider: method.Kind == PaymentMethodKind.MobileMoney ? method.Provider : null,
                Msisdn: method.Kind == PaymentMethodKind.MobileMoney
                    ? protector.Unprotect(subscription.TenantId, method.EncryptedPhoneNumber!) : null,
                Reference: attempt.Reference), cancellationToken);
            if (charge.Reference != attempt.Reference)
                throw new InvalidOperationException("Paystack returned an unexpected renewal reference.");
            if (charge.Status is "pay_offline" or "pending")
            {
                attempt.Status = PaymentAttemptStatus.AwaitingApproval;
                attempt.ApprovalMessage = charge.DisplayText;
                if (method.Kind == PaymentMethodKind.MobileMoney && start <= now)
                {
                    period.MarkPastDue();
                    SubscriptionStateMachine.RecordChargeFailure(subscription, start);
                }
            }
            else if (charge.Status == "failed")
            {
                BillingMetrics.RenewalFailures.Add(1,
                    new KeyValuePair<string, object?>("outcome", "provider_failed"));
                attempt.MarkFailed(now, "provider_failed");
                intent.MarkFailed();
                period.MarkPastDue();
                SubscriptionStateMachine.RecordChargeFailure(subscription, start);
            }
            else
            {
                attempt.MarkUnknown();
                await context.SaveChangesAsync(cancellationToken);
                await VerifyAttemptAsync(context, gateway, settlement, subscription, period, intent,
                    attempt, now, cancellationToken);
            }
        }
        catch
        {
            attempt.MarkUnknown();
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task VerifyAttemptAsync(AppDbContext context, IPaymentGateway gateway,
        IBillingSettlementService settlement, Subscription subscription, BillingPeriod period,
        PaymentIntent intent, PaymentAttempt attempt, DateTime now, CancellationToken cancellationToken)
    {
        PaymentVerificationResult verified;
        try { verified = await gateway.VerifyAsync(attempt.Reference, cancellationToken); }
        catch { attempt.MarkUnknown(); return; }
        if (verified.Reference != attempt.Reference || verified.Amount != attempt.Amount ||
            !string.Equals(verified.Currency, attempt.Currency, StringComparison.OrdinalIgnoreCase))
        {
            attempt.MarkUnknown();
            return;
        }
        if (verified.Status == "success")
            await settlement.SettleAsync(attempt.Reference, verified.Amount, verified.Currency,
                verified.PaidAt ?? now, cancellationToken);
        else if (verified.Status == "failed")
        {
            attempt.MarkFailed(now, "provider_failed");
            intent.MarkFailed();
            period.MarkPastDue();
            SubscriptionStateMachine.RecordChargeFailure(subscription, period.StartAt);
        }
    }

    private static void Enqueue(AppDbContext context, Guid tenantId, string type, string payload) => context.OutboxMessages.Add(new OutboxMessage
    { TenantId = tenantId, Type = type, Payload = payload });
}
