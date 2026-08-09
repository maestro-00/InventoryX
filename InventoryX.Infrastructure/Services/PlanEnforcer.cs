using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>
    /// Plan enforcement against the tenant's subscription (FR-009/010):
    /// entity/sale caps, feature gates and the ReadOnly write-block.
    /// </summary>
    public class PlanEnforcer(AppDbContext context, ITenantContext tenantContext) : IPlanEnforcer
    {
        private Subscription? _subscription;

        private async Task<Subscription?> GetSubscriptionAsync(CancellationToken cancellationToken)
        {
            if (_subscription is not null) return _subscription;
            _subscription = await context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status != SubscriptionStatus.Cancelled && s.Status != SubscriptionStatus.PurgePending)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            return _subscription;
        }

        public async Task EnsureWritableAsync(CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            if (subscription?.Status == SubscriptionStatus.ReadOnly)
                throw new PlanLimitException(
                    "Your subscription is in read-only mode. Renew your plan to resume making changes.",
                    "Settle the outstanding payment or choose a plan under /billing/plans.");
        }

        public async Task EnsureFeatureAsync(PlanFeature feature, CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            var plan = subscription?.Plan;
            if (plan is null) return;

            var enabled = feature switch
            {
                PlanFeature.PurchaseOrders => plan.PurchaseOrders,
                PlanFeature.BatchExpiry => plan.BatchExpiry,
                PlanFeature.Serials => plan.Serials,
                PlanFeature.MultiCurrency => plan.MultiCurrency,
                PlanFeature.CustomRoles => plan.CustomRoles,
                PlanFeature.AdvancedReports => plan.AdvancedReports,
                PlanFeature.Integrations => plan.Integrations,
                _ => true,
            };

            if (!enabled)
                throw new PlanLimitException(
                    $"The {feature} feature is not included in your {plan.Name} plan.",
                    "Upgrade your plan under /billing/plans to unlock this feature.");
        }

        public async Task EnsureWithinLimitAsync(UsageMetric metric, int increment = 1, CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            var plan = subscription?.Plan;
            if (plan is null) return;

            int? limit = metric switch
            {
                UsageMetric.SalesThisMonth => plan.MonthlySaleCap,
                UsageMetric.Products => plan.MaxProducts,
                UsageMetric.Users => plan.MaxUsers,
                UsageMetric.Locations => plan.MaxLocations,
                UsageMetric.Registers => plan.MaxRegisters,
                _ => null,
            };
            if (limit is null) return;

            var periodKey = PeriodKeyFor(metric);
            var current = await context.UsageCounters
                .Where(c => c.Metric == metric && c.PeriodKey == periodKey)
                .Select(c => (int?)c.Count)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            if (current + increment > limit)
                throw new PlanLimitException(
                    $"Your {plan.Name} plan allows {limit} {MetricLabel(metric)}; you have used {current}.",
                    $"Upgrade your plan under /billing/plans to raise the {MetricLabel(metric)} limit.");
        }

        public async Task IncrementUsageAsync(UsageMetric metric, int delta = 1, CancellationToken cancellationToken = default)
        {
            var periodKey = PeriodKeyFor(metric);
            var counter = await context.UsageCounters
                .FirstOrDefaultAsync(c => c.Metric == metric && c.PeriodKey == periodKey, cancellationToken);

            if (counter is null)
            {
                counter = new UsageCounter { Metric = metric, PeriodKey = periodKey, Count = 0 };
                context.UsageCounters.Add(counter);
            }

            counter.Count = Math.Max(0, counter.Count + delta);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static string PeriodKeyFor(UsageMetric metric) =>
            metric == UsageMetric.SalesThisMonth ? DateTime.UtcNow.ToString("yyyy-MM") : "*";

        private static string MetricLabel(UsageMetric metric) => metric switch
        {
            UsageMetric.SalesThisMonth => "sales this month",
            UsageMetric.Products => "products",
            UsageMetric.Users => "users",
            UsageMetric.Locations => "locations",
            UsageMetric.Registers => "registers",
            _ => metric.ToString(),
        };
    }
}
