using System.Globalization;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

public sealed class QuoteBillingCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<QuoteBillingCommand, BillingQuoteDto>
{
    public async Task<BillingQuoteDto> Handle(QuoteBillingCommand request, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is null || tenantContext.Role != "Owner")
            throw new UnauthorizedAccessException("Only the business owner can quote billing changes.");
        var subscription = await context.Subscriptions.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Subscription not found.");
        var plan = await context.PlanDefinitions.SingleOrDefaultAsync(p => p.Id == request.TargetPlanId && p.IsActive,
            cancellationToken) ?? throw new NotFoundException("Published plan not found.");
        if (plan.Tier is PlanTier.Free or PlanTier.Enterprise)
            throw new ConflictException("Choose a published self-service paid plan.");
        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.PurgePending)
            throw new ConflictException("The subscription must be recovered before purchasing.");

        var price = request.BillingCycle == BillingCycle.Annual ? plan.AnnualPrice : plan.MonthlyPrice;
        if (price <= 0) throw new ConflictException("The selected plan has no published price.");
        var now = DateTime.UtcNow;
        var isScheduled = request.Purpose is PaymentIntentPurpose.Downgrade or PaymentIntentPurpose.CycleSwitch;
        var currentPlan = subscription.Plan ?? await context.PlanDefinitions.SingleAsync(p => p.Id == subscription.PlanDefinitionId, cancellationToken);
        var currentPrice = subscription.BillingCycle == BillingCycle.Annual ? currentPlan.AnnualPrice : currentPlan.MonthlyPrice;
        var nextRenewal = BillingPeriodService.Boundary(new DateTimeOffset(now), request.BillingCycle, 1).UtcDateTime;
        var effectiveAt = isScheduled ? subscription.CurrentPeriodEnd : now;
        var remainingRatio = subscription.CurrentPeriodEnd > subscription.CurrentPeriodStart
            ? Math.Clamp((decimal)(subscription.CurrentPeriodEnd - now).TotalSeconds /
                (decimal)(subscription.CurrentPeriodEnd - subscription.CurrentPeriodStart).TotalSeconds, 0m, 1m) : 0m;
        var prorationCredit = request.Purpose == PaymentIntentPurpose.Upgrade
            ? Math.Max(0m, currentPrice - price) * remainingRatio : 0m;
        var prorationCharge = request.Purpose == PaymentIntentPurpose.Upgrade
            ? Math.Max(0m, price - currentPrice) * remainingRatio : price;
        var exceeded = new List<string>();
        if (request.Purpose == PaymentIntentPurpose.Downgrade)
        {
            if (plan.MaxProducts is int products && await BillingUsageReader.CurrentAsync(context, UsageMetric.Products, subscription.TenantId, cancellationToken) > products) exceeded.Add("products");
            if (plan.MaxLocations is int locations && await BillingUsageReader.CurrentAsync(context, UsageMetric.Locations, subscription.TenantId, cancellationToken) > locations) exceeded.Add("locations");
            if (plan.MaxUsers is int users && await BillingUsageReader.CurrentAsync(context, UsageMetric.Users, subscription.TenantId, cancellationToken) > users) exceeded.Add("users");
            if (plan.MaxRegisters is int registers && await BillingUsageReader.CurrentAsync(context, UsageMetric.Registers, subscription.TenantId, cancellationToken) > registers) exceeded.Add("registers");
        }
        // The catalogue currently has no billing-tax rate. Keep the explicit snapshot at zero
        // until a configured billing tax policy can supply an applicable rate.
        const decimal tax = 0m;
        var quote = new BillingQuote
        {
            TenantId = tenantContext.TenantId.Value,
            SubscriptionId = subscription.Id,
            TargetPlanDefinitionId = plan.Id,
            TargetCycle = request.BillingCycle,
            Purpose = request.Purpose,
            PriceVersion = PriceVersion(plan, request.BillingCycle),
            BaseAmount = prorationCharge,
            TaxAmount = tax,
            TotalAmount = prorationCharge + tax,
            ProrationCredit = prorationCredit,
            EffectiveAt = effectiveAt,
            NextRenewalAt = nextRenewal,
            NextRenewalTotal = price + tax,
            ExpiresAt = now.AddMinutes(15),
            ExceededLimits = System.Text.Json.JsonSerializer.Serialize(exceeded),
            RequiresAcknowledgement = exceeded.Count > 0,
        };
        context.BillingQuotes.Add(quote);
        await context.SaveChangesAsync(cancellationToken);
        return new BillingQuoteDto(quote.Id, quote.ExpiresAt, quote.Currency, quote.BaseAmount,
            quote.TaxAmount, quote.TotalAmount, quote.ProrationCredit, quote.EffectiveAt, quote.NextRenewalAt,
            quote.NextRenewalTotal, exceeded, quote.RequiresAcknowledgement);
    }

    public static string PriceVersion(PlanDefinition plan, BillingCycle cycle) =>
        $"{plan.Id:N}:{cycle}:{(cycle == BillingCycle.Annual ? plan.AnnualPrice : plan.MonthlyPrice).ToString(CultureInfo.InvariantCulture)}";
}
