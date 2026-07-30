using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Billing;

public sealed class GetBillingPlansQueryHandler(IAppDbContext context)
    : IRequestHandler<GetBillingPlansQuery, List<BillingPlanDto>>
{
    public async Task<List<BillingPlanDto>> Handle(GetBillingPlansQuery request, CancellationToken cancellationToken) =>
        (await context.PlanDefinitions.AsNoTracking().Where(plan => plan.IsActive)
            .ToListAsync(cancellationToken)).OrderBy(plan => plan.MonthlyPrice).Select(ToDto).ToList();

    internal static BillingPlanDto ToDto(PlanDefinition plan) => new(
        plan.Id, plan.Tier.ToString(), plan.Name, plan.MonthlyPrice, plan.AnnualPrice,
        plan.MaxLocations, plan.MaxUsers, plan.MaxProducts, plan.MaxRegisters, plan.MonthlySaleCap,
        new Dictionary<string, bool>
        {
            ["purchaseOrders"] = plan.PurchaseOrders, ["batchExpiry"] = plan.BatchExpiry,
            ["serials"] = plan.Serials, ["multiCurrency"] = plan.MultiCurrency,
            ["customRoles"] = plan.CustomRoles, ["advancedReports"] = plan.AdvancedReports,
            ["integrations"] = plan.Integrations,
        });
}

public sealed class GetCurrentBillingSubscriptionQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCurrentBillingSubscriptionQuery, BillingSubscriptionDto>
{
    public async Task<BillingSubscriptionDto> Handle(GetCurrentBillingSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var subscription = await context.Subscriptions.AsNoTracking().Include(item => item.Plan)
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Subscription not found.");
        var counters = await context.UsageCounters.AsNoTracking().ToListAsync(cancellationToken);
        var periodKey = DateTime.UtcNow.ToString("yyyy-MM");
        int Usage(UsageMetric metric) => counters.FirstOrDefault(counter => counter.Metric == metric &&
            counter.PeriodKey == (metric == UsageMetric.SalesThisMonth ? periodKey : "*"))?.Count ?? 0;
        var plan = subscription.Plan ?? throw new NotFoundException("Subscription plan not found.");
        return new BillingSubscriptionDto(subscription.Id, plan.Name, subscription.Status.ToString(), subscription.BillingCycle.ToString(),
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.TrialEndsAt, subscription.GraceExpiresAt,
            subscription.CancelledAt, subscription.PurgeAt,
            [
                new UsageVsLimitDto("salesThisMonth", Usage(UsageMetric.SalesThisMonth), plan.MonthlySaleCap),
                new UsageVsLimitDto("locations", Usage(UsageMetric.Locations), plan.MaxLocations),
                new UsageVsLimitDto("users", Usage(UsageMetric.Users), plan.MaxUsers),
                new UsageVsLimitDto("products", Usage(UsageMetric.Products), plan.MaxProducts),
                new UsageVsLimitDto("registers", Usage(UsageMetric.Registers), plan.MaxRegisters),
            ]);
    }
}
