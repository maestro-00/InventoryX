using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
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
        var now = DateTime.UtcNow;
        var subscriptions = await context.Subscriptions.IgnoreQueryFilters().Include(item => item.Plan).ToListAsync(cancellationToken);
        var freePlan = await context.PlanDefinitions.FirstOrDefaultAsync(item => item.Tier == PlanTier.Free, cancellationToken);

        foreach (var subscription in subscriptions)
        {
            if (subscription.Status == SubscriptionStatus.Trialing && freePlan is not null)
                SubscriptionStateMachine.ExpireTrialToFree(subscription, freePlan.Id, now);
            if (subscription.Status == SubscriptionStatus.PastDue)
            {
                if (subscription.GraceExpiresAt <= now) SubscriptionStateMachine.ExhaustGrace(subscription, now);
                else if (subscription.LastChargeAttemptAt is null || subscription.LastChargeAttemptAt <= now.AddDays(-1))
                    await RetryChargeAsync(context, gateway, subscription, now, cancellationToken);
            }
            else if (subscription.Status == SubscriptionStatus.Active && subscription.CancelledAt is null && subscription.CurrentPeriodEnd <= now)
                await RetryChargeAsync(context, gateway, subscription, now, cancellationToken);
            else if (subscription.CancelledAt <= now && subscription.Status != SubscriptionStatus.PurgePending)
                SubscriptionStateMachine.StartPurgeClock(subscription, now);

            if (subscription.Status == SubscriptionStatus.PurgePending && subscription.PurgeAt <= now)
                Enqueue(context, subscription.TenantId, "tenant.purge.requested", $"{{\"tenantId\":\"{subscription.TenantId}\"}}");
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task RetryChargeAsync(AppDbContext context, IPaymentGateway gateway, Subscription subscription, DateTime now, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
        var amount = subscription.BillingCycle == BillingCycle.Annual ? subscription.Plan!.AnnualPrice : subscription.Plan!.MonthlyPrice;
        try
        {
            if (string.IsNullOrWhiteSpace(subscription.PaymentMethodRef) || string.IsNullOrWhiteSpace(tenant.BillingEmail))
                throw new InvalidOperationException("No billing authorization is available.");
            var charge = await gateway.ChargeAsync(new PaymentChargeRequest(tenant.BillingEmail, amount, tenant.Currency,
                AuthorizationCode: subscription.PaymentMethodKind == "card" ? subscription.PaymentMethodRef : null,
                MobileMoneyProvider: subscription.PaymentProvider, Reference: $"renewal-{subscription.Id:N}-{now:yyyyMMdd}"), cancellationToken);
            if (charge.Status.Equals("success", StringComparison.OrdinalIgnoreCase)) SubscriptionStateMachine.RecordChargeSuccess(subscription, now);
            else throw new InvalidOperationException(charge.DisplayText ?? "Charge was not successful.");
        }
        catch (Exception exception)
        {
            SubscriptionStateMachine.RecordChargeFailure(subscription, now);
            context.Notifications.Add(new Notification
            {
                TenantId = subscription.TenantId, Type = NotificationType.BillingFailure, Channel = NotificationChannel.InApp,
                ConsolidationKey = $"billing-failure:{subscription.Id}", Title = "Subscription payment needs attention",
                Message = $"Attempt {subscription.FailedChargeAttempts} of 7 failed: {exception.Message}", LastRaisedAt = now,
            });
            Enqueue(context, subscription.TenantId, "billing.payment.failed", $"{{\"subscriptionId\":\"{subscription.Id}\"}}");
        }
    }

    private static void Enqueue(AppDbContext context, Guid tenantId, string type, string payload) => context.OutboxMessages.Add(new OutboxMessage
    { TenantId = tenantId, Type = type, Payload = payload });
}
