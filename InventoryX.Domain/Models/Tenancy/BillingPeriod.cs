using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum BillingPeriodStatus { Pending, Paid, PastDue, Waived }

/// <summary>A single half-open calendar period; its price is fixed when created.</summary>
public sealed class BillingPeriod : BaseModel
{
    public Guid SubscriptionId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public BillingCycle Cycle { get; set; }
    public Guid PlanDefinitionId { get; set; }
    public string PriceVersion { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "GHS";
    public BillingPeriodStatus Status { get; private set; } = BillingPeriodStatus.Pending;
    public DateTime? PaidAt { get; private set; }
    public DateTime? WaivedAt { get; private set; }
    public string? WaiverReason { get; private set; }

    public void MarkPaid(DateTime confirmedAt)
    {
        if (Status == BillingPeriodStatus.Waived)
            throw new InvalidOperationException("A waived period cannot be collected.");
        if (Status == BillingPeriodStatus.Paid) return;
        if (EndAt <= StartAt) throw new InvalidOperationException("The billing period has invalid boundaries.");
        Status = BillingPeriodStatus.Paid;
        PaidAt = confirmedAt;
    }

    public void MarkPastDue()
    {
        if (Status == BillingPeriodStatus.Pending) Status = BillingPeriodStatus.PastDue;
    }

    public void Waive(DateTime now, string reason)
    {
        if (Status == BillingPeriodStatus.Paid)
            throw new InvalidOperationException("A paid period cannot be waived.");
        if (Status == BillingPeriodStatus.Waived) return;
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A waiver reason is required.", nameof(reason));
        Status = BillingPeriodStatus.Waived;
        WaivedAt = now;
        WaiverReason = reason;
    }
}
