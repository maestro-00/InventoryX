using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum ScheduledChangeKind { Downgrade, CycleSwitch, Cancel }
public enum ScheduledChangeStatus { Scheduled, Reversed, Applied }

public sealed class ScheduledSubscriptionChange : BaseModel
{
    public Guid SubscriptionId { get; set; }
    public ScheduledChangeKind Kind { get; set; }
    public Guid? TargetPlanDefinitionId { get; set; }
    public BillingCycle? TargetCycle { get; set; }
    public decimal? QuotedNextPeriodAmount { get; set; }
    public DateTime EffectiveAt { get; set; }
    public string ExceededLimitsJson { get; set; } = "[]";
    public DateTime? AcknowledgedAt { get; set; }
    public ScheduledChangeStatus Status { get; private set; } = ScheduledChangeStatus.Scheduled;

    public void Reverse()
    {
        if (Status == ScheduledChangeStatus.Applied) throw new InvalidOperationException("An applied change cannot be reversed.");
        Status = ScheduledChangeStatus.Reversed;
    }

    public void Apply() => Status = ScheduledChangeStatus.Applied;
}
