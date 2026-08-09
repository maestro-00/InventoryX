using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy
{
    public enum SubscriptionStatus { Trialing, Active, PastDue, ReadOnly, Cancelled, PurgePending }

    public enum BillingCycle { Monthly, Annual }

    /// <summary>
    /// The tenant's commercial state machine (research R7):
    /// Trialing → Active → PastDue(≤7d grace) → ReadOnly → Cancelled → PurgePending.
    /// Trial expiry without payment falls back to the Free plan as Active.
    /// </summary>
    public class Subscription : BaseModel
    {
        public Guid PlanDefinitionId { get; set; }
        public PlanDefinition? Plan { get; set; }
        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

        public DateTime? TrialEndsAt { get; set; }
        public DateTime CurrentPeriodStart { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public DateTime? GraceExpiresAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? PurgeAt { get; set; }

        /// <summary>Pending period-end downgrade target (null = none).</summary>
        public Guid? PendingPlanDefinitionId { get; set; }

        /// <summary>Paystack authorization code — never raw card data (FR-059).</summary>
        public string? PaymentMethodRef { get; set; }
        /// <summary>card | mobile_money.</summary>
        public string? PaymentMethodKind { get; set; }
        /// <summary>MoMo provider (mtn|telecel|at) when mobile money.</summary>
        public string? PaymentProvider { get; set; }
        public int FailedChargeAttempts { get; set; }
        public DateTime? LastChargeAttemptAt { get; set; }
    }
}
