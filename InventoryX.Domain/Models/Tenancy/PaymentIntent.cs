using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum PaymentIntentPurpose { Initial, Renewal, Upgrade, Recovery, MethodVerification }
public enum PaymentIntentStatus { Pending, Succeeded, Failed, Cancelled, Superseded }

/// <summary>One owner action or payable period, independent of provider retries.</summary>
public sealed class PaymentIntent : BaseModel
{
    public Guid SubscriptionId { get; set; }
    public Guid? BillingPeriodId { get; set; }
    public PaymentIntentPurpose Purpose { get; set; }
    public Guid? TargetPlanDefinitionId { get; set; }
    public BillingCycle? TargetCycle { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "GHS";
    public DateTime QuoteExpiresAt { get; set; }
    public DateTime? ExpectedPeriodStart { get; set; }
    public DateTime? ExpectedPeriodEnd { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public PaymentIntentStatus Status { get; private set; } = PaymentIntentStatus.Pending;
    public DateTime? SettledAt { get; private set; }

    public bool CanStartAttempt(IEnumerable<PaymentAttempt> attempts) =>
        Status is PaymentIntentStatus.Pending or PaymentIntentStatus.Failed &&
        !attempts.Any(attempt => attempt.Status is PaymentAttemptStatus.Created or
            PaymentAttemptStatus.AwaitingApproval or PaymentAttemptStatus.Unknown or PaymentAttemptStatus.Succeeded);

    public void MarkFailed()
    {
        if (Status == PaymentIntentStatus.Pending) Status = PaymentIntentStatus.Failed;
    }

    public void MarkSucceeded(DateTime confirmedAt)
    {
        if (Status == PaymentIntentStatus.Succeeded) return;
        if (Status is PaymentIntentStatus.Cancelled or PaymentIntentStatus.Superseded)
            throw new InvalidOperationException("A cancelled or superseded intent cannot settle.");
        Status = PaymentIntentStatus.Succeeded;
        SettledAt = confirmedAt;
    }
}
