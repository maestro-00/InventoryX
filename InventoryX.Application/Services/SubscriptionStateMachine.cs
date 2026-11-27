using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services;

/// <summary>Pure subscription lifecycle transitions; persistence and payment calls remain in handlers/workers.</summary>
public static class SubscriptionStateMachine
{
    /// <summary>
    /// Applies a provider-verified result to the tracked ledger in one persistence transaction.
    /// Callers must save the entities together with the invoice and commit atomically.
    /// </summary>
    public static bool SettleVerifiedPayment(
        Subscription subscription, BillingPeriod period, PaymentIntent intent,
        PaymentAttempt attempt, decimal verifiedAmount, string verifiedCurrency, DateTime confirmedAt)
    {
        if (intent.Status == PaymentIntentStatus.Succeeded) return false;
        if (subscription.TenantId != period.TenantId || subscription.TenantId != intent.TenantId ||
            subscription.TenantId != attempt.TenantId || period.SubscriptionId != subscription.Id ||
            intent.SubscriptionId != subscription.Id || intent.BillingPeriodId != period.Id ||
            attempt.PaymentIntentId != intent.Id)
            throw new InvalidOperationException("Billing ledger ownership does not match.");
        if (string.IsNullOrWhiteSpace(attempt.Reference) || verifiedAmount != attempt.Amount ||
            verifiedAmount != intent.TotalAmount ||
            (intent.Purpose != PaymentIntentPurpose.Upgrade && verifiedAmount != period.TotalAmount) ||
            !string.Equals(verifiedCurrency, attempt.Currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(verifiedCurrency, intent.Currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(verifiedCurrency, period.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Verified payment does not match the billing snapshot.");
        if (period.EndAt <= period.StartAt || period.Status == BillingPeriodStatus.Waived ||
            (intent.Purpose == PaymentIntentPurpose.Upgrade && period.Status != BillingPeriodStatus.Paid) ||
            (period.Status == BillingPeriodStatus.Paid && intent.Purpose != PaymentIntentPurpose.Upgrade))
            throw new InvalidOperationException("The billing period cannot accept this payment.");
        if (intent.Status is PaymentIntentStatus.Cancelled or PaymentIntentStatus.Superseded)
            throw new InvalidOperationException("The payment intent is no longer collectible.");
        if (intent.Purpose is PaymentIntentPurpose.Initial or PaymentIntentPurpose.Upgrade &&
            intent.TargetPlanDefinitionId is null)
            throw new InvalidOperationException("A plan purchase requires a target plan.");
        if (intent.Purpose == PaymentIntentPurpose.Initial && intent.TargetCycle is null)
            throw new InvalidOperationException("An initial purchase requires a billing cycle.");

        attempt.MarkSucceeded(confirmedAt);
        intent.MarkSucceeded(confirmedAt);
        if (intent.Purpose != PaymentIntentPurpose.Upgrade) period.MarkPaid(confirmedAt);
        switch (intent.Purpose)
        {
            case PaymentIntentPurpose.Initial:
                subscription.PlanDefinitionId = intent.TargetPlanDefinitionId!.Value;
                subscription.BillingCycle = intent.TargetCycle!.Value;
                subscription.BillingAnchor = period.StartAt;
                subscription.CurrentPeriodStart = period.StartAt;
                subscription.CurrentPeriodEnd = period.EndAt;
                break;
            case PaymentIntentPurpose.Upgrade:
                subscription.PlanDefinitionId = intent.TargetPlanDefinitionId!.Value;
                break;
            case PaymentIntentPurpose.Renewal:
            case PaymentIntentPurpose.Recovery:
                subscription.CurrentPeriodStart = period.StartAt;
                subscription.CurrentPeriodEnd = period.EndAt;
                break;
        }
        RecordChargeSuccess(subscription, confirmedAt);
        return true;
    }

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
