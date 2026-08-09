using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services;

/// <summary>Pure subscription lifecycle transitions; persistence and payment calls remain in handlers/workers.</summary>
public static class SubscriptionStateMachine
{
    public static void ExpireTrialToFree(Subscription subscription, Guid freePlanId, DateTime now)
    {
        if (subscription.Status != SubscriptionStatus.Trialing || subscription.TrialEndsAt is null || subscription.TrialEndsAt > now) return;
        subscription.PlanDefinitionId = freePlanId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.TrialEndsAt = null;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = now.AddMonths(1);
    }

    public static void ActivatePaidPlan(Subscription subscription, Guid planId, BillingCycle cycle, DateTime now)
    {
        subscription.PlanDefinitionId = planId;
        subscription.BillingCycle = cycle;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = cycle == BillingCycle.Annual ? now.AddYears(1) : now.AddMonths(1);
        subscription.GraceExpiresAt = null;
        subscription.FailedChargeAttempts = 0;
    }

    public static void RecordChargeFailure(Subscription subscription, DateTime now)
    {
        subscription.Status = SubscriptionStatus.PastDue;
        subscription.GraceExpiresAt ??= now.AddDays(7);
        subscription.FailedChargeAttempts++;
        subscription.LastChargeAttemptAt = now;
    }

    public static void RecordChargeSuccess(Subscription subscription, DateTime now)
    {
        subscription.Status = SubscriptionStatus.Active;
        subscription.GraceExpiresAt = null;
        subscription.FailedChargeAttempts = 0;
        subscription.LastChargeAttemptAt = now;
    }

    public static void ExhaustGrace(Subscription subscription, DateTime now)
    {
        if (subscription.Status == SubscriptionStatus.PastDue && subscription.GraceExpiresAt <= now)
            subscription.Status = SubscriptionStatus.ReadOnly;
    }

    public static void CancelAtPeriodEnd(Subscription subscription, DateTime now)
    {
        subscription.CancelledAt = subscription.CurrentPeriodEnd > now ? subscription.CurrentPeriodEnd : now;
        subscription.PurgeAt = subscription.CancelledAt.Value.AddDays(90);
    }

    public static void Reactivate(Subscription subscription, DateTime now)
    {
        if (subscription.PurgeAt is not null && subscription.PurgeAt <= now)
            throw new InvalidOperationException("The cancellation retention period has expired.");
        subscription.Status = SubscriptionStatus.Active;
        subscription.CancelledAt = null;
        subscription.PurgeAt = null;
    }

    public static void StartPurgeClock(Subscription subscription, DateTime now)
    {
        if (subscription.CancelledAt is null) subscription.CancelledAt = now;
        subscription.PurgeAt ??= subscription.CancelledAt.Value.AddDays(90);
        subscription.Status = SubscriptionStatus.PurgePending;
    }
}
