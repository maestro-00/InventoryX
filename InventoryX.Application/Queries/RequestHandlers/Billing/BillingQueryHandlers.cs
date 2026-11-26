using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
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
        var sales = await BillingUsageReader.CurrentAsync(context, UsageMetric.SalesThisMonth, subscription.TenantId, cancellationToken);
        var locations = await BillingUsageReader.CurrentAsync(context, UsageMetric.Locations, subscription.TenantId, cancellationToken);
        var users = await BillingUsageReader.CurrentAsync(context, UsageMetric.Users, subscription.TenantId, cancellationToken);
        var products = await BillingUsageReader.CurrentAsync(context, UsageMetric.Products, subscription.TenantId, cancellationToken);
        var registers = await BillingUsageReader.CurrentAsync(context, UsageMetric.Registers, subscription.TenantId, cancellationToken);
        var plan = subscription.Plan ?? throw new NotFoundException("Subscription plan not found.");
        return new BillingSubscriptionDto(subscription.Id, plan.Name, subscription.Status.ToString(), subscription.BillingCycle.ToString(),
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.TrialEndsAt, subscription.GraceExpiresAt,
            subscription.CancelledAt, subscription.PurgeAt,
            [
                new UsageVsLimitDto("salesThisMonth", sales, plan.MonthlySaleCap),
                new UsageVsLimitDto("locations", locations, plan.MaxLocations),
                new UsageVsLimitDto("users", users, plan.MaxUsers),
                new UsageVsLimitDto("products", products, plan.MaxProducts),
                new UsageVsLimitDto("registers", registers, plan.MaxRegisters),
            ]);
    }
}
