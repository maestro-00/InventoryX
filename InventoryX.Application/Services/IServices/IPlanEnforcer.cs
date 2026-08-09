using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services.IServices
{
    /// <summary>Plan feature gates checked by the enforcement behavior (FR-009/010).</summary>
    public enum PlanFeature { PurchaseOrders, BatchExpiry, Serials, MultiCurrency, CustomRoles, AdvancedReports, Integrations }

    /// <summary>
    /// Consulted by PlanEnforcementBehavior before commands run: entity/usage
    /// limits, feature gates and the ReadOnly write-block. Violations throw
    /// PlanLimitException (→ 402 problem with upgrade hint).
    /// </summary>
    public interface IPlanEnforcer
    {
        /// <summary>Throws PlanLimitException if adding <paramref name="increment"/> to the metric would exceed the plan limit.</summary>
        Task EnsureWithinLimitAsync(UsageMetric metric, int increment = 1, CancellationToken cancellationToken = default);

        /// <summary>Throws PlanLimitException if the tenant's plan does not include the feature.</summary>
        Task EnsureFeatureAsync(PlanFeature feature, CancellationToken cancellationToken = default);

        /// <summary>Throws PlanLimitException if the subscription is ReadOnly (export/billing exempt).</summary>
        Task EnsureWritableAsync(CancellationToken cancellationToken = default);

        /// <summary>Transactionally increments a usage counter after a successful create/sale.</summary>
        Task IncrementUsageAsync(UsageMetric metric, int delta = 1, CancellationToken cancellationToken = default);
    }
}
