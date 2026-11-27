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
        }, "GHS", Math.Max(0m, plan.MonthlyPrice * 12m - plan.AnnualPrice),
        plan.Tier == PlanTier.Enterprise);
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
        var activeMethod = subscription.CurrentPaymentMethodId is { } methodId
            ? await context.PaymentMethods.AsNoTracking().SingleOrDefaultAsync(method =>
                method.Id == methodId && method.Status == PaymentMethodStatus.Active, cancellationToken)
            : null;
        var scheduled = await context.ScheduledSubscriptionChanges.AsNoTracking()
            .Where(change => change.SubscriptionId == subscription.Id && change.Status == ScheduledChangeStatus.Scheduled)
            .OrderByDescending(change => change.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var nextRenewalAt = subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue
            ? subscription.CurrentPeriodEnd : (DateTime?)null;
        decimal? nextRenewalAmount = nextRenewalAt is null ? null :
            subscription.BillingCycle == BillingCycle.Annual ? plan.AnnualPrice : plan.MonthlyPrice;
        return new BillingSubscriptionDto(subscription.Id, plan.Name, subscription.Status.ToString(), subscription.BillingCycle.ToString(),
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.TrialEndsAt, subscription.GraceExpiresAt,
            subscription.CancelledAt, subscription.PurgeAt,
            [
                new UsageVsLimitDto("salesThisMonth", sales, plan.MonthlySaleCap),
                new UsageVsLimitDto("locations", locations, plan.MaxLocations),
                new UsageVsLimitDto("users", users, plan.MaxUsers),
                new UsageVsLimitDto("products", products, plan.MaxProducts),
                new UsageVsLimitDto("registers", registers, plan.MaxRegisters),
            ], nextRenewalAt, nextRenewalAmount,
            activeMethod is null ? null : new BillingMethodSummaryDto(activeMethod.Id,
                activeMethod.Kind == PaymentMethodKind.Card ? "card" : "mobile_money",
                activeMethod.Provider, activeMethod.MaskedIdentifier, activeMethod.Brand,
                activeMethod.ExpiryMonth, activeMethod.ExpiryYear, activeMethod.RenewalMode.ToString()),
            subscription.Status is not (SubscriptionStatus.ReadOnly or SubscriptionStatus.Cancelled or SubscriptionStatus.PurgePending),
            subscription.Status == SubscriptionStatus.ReadOnly
                ? "Your paid mutations are locked until the outstanding billing period is recovered."
                : null,
            scheduled?.Id, scheduled?.Kind.ToString(), scheduled?.EffectiveAt,
            subscription.Status == SubscriptionStatus.PastDue ? nextRenewalAmount : null,
            subscription.LastChargeAttemptAt, subscription.FailedChargeAttempts,
            subscription.Status == SubscriptionStatus.PastDue ? "retrying" : null);
    }
}

public sealed class GetBillingPaymentMethodsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetBillingPaymentMethodsQuery, List<BillingPaymentMethodDto>>
{
    public async Task<List<BillingPaymentMethodDto>> Handle(GetBillingPaymentMethodsQuery request,
        CancellationToken cancellationToken) =>
        await context.PaymentMethods.AsNoTracking()
            .Where(method => method.Status == PaymentMethodStatus.Active ||
                method.Status == PaymentMethodStatus.Pending)
            .OrderByDescending(method => method.CreatedAt)
            .Select(method => new BillingPaymentMethodDto(method.Id,
                method.Kind == PaymentMethodKind.Card ? "card" : "mobile_money",
                method.Status.ToString(), method.Provider, method.MaskedIdentifier,
                method.Brand, method.ExpiryMonth, method.ExpiryYear,
                method.RenewalMode.ToString()))
            .ToListAsync(cancellationToken);
}
