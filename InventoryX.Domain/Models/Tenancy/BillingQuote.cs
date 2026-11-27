using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

/// <summary>A short-lived, tenant-owned snapshot of a proposed billing action.</summary>
public sealed class BillingQuote : BaseModel
{
    public Guid SubscriptionId { get; set; }
    public Guid TargetPlanDefinitionId { get; set; }
    public BillingCycle TargetCycle { get; set; }
    public PaymentIntentPurpose Purpose { get; set; }
    public string PriceVersion { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ProrationCredit { get; set; }
    public string Currency { get; set; } = "GHS";
    public DateTime EffectiveAt { get; set; }
    public DateTime NextRenewalAt { get; set; }
    public decimal NextRenewalTotal { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string ExceededLimits { get; set; } = "[]";
    public bool RequiresAcknowledgement { get; set; }
}
