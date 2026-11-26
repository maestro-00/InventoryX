using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.RequestHandlers.Billing;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

internal static class SubscriptionCommands
{
    public static async Task<Subscription> CurrentAsync(IAppDbContext context, CancellationToken cancellationToken) =>
        await context.Subscriptions.Include(item => item.Plan).OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Subscription not found.");

    public static BillingSubscriptionDto ToDto(Subscription subscription) => new(
        subscription.Id, subscription.Plan?.Name ?? string.Empty, subscription.Status.ToString(), subscription.BillingCycle.ToString(),
        subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.TrialEndsAt, subscription.GraceExpiresAt,
        subscription.CancelledAt, subscription.PurgeAt, []);
}

public sealed class UpgradeSubscriptionCommandHandler(IAppDbContext context, IPaymentGateway paymentGateway)
    : IRequestHandler<UpgradeSubscriptionCommand, BillingSubscriptionDto>
{
    public async Task<BillingSubscriptionDto> Handle(UpgradeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        var target = await context.PlanDefinitions.SingleOrDefaultAsync(plan => plan.Id == request.PlanDefinitionId && plan.IsActive, cancellationToken)
            ?? throw new NotFoundException("Target plan not found.");
        var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
        if (subscription.PaymentMethodRef is null || string.IsNullOrWhiteSpace(tenant.BillingEmail))
            throw new FluentValidation.ValidationException("Add a billing email and payment method before switching to a paid plan.");
        var currentPrice = subscription.BillingCycle == BillingCycle.Annual ? subscription.Plan!.AnnualPrice : subscription.Plan!.MonthlyPrice;
        var targetPrice = request.BillingCycle == BillingCycle.Annual ? target.AnnualPrice : target.MonthlyPrice;
        var remaining = Math.Clamp((subscription.CurrentPeriodEnd - DateTime.UtcNow).TotalDays / Math.Max(1, (subscription.CurrentPeriodEnd - subscription.CurrentPeriodStart).TotalDays), 0, 1);
        var prorated = Math.Round(Math.Max(0, targetPrice - currentPrice) * (decimal)remaining, 2);
        if (prorated > 0)
        {
            var charge = await paymentGateway.ChargeAsync(new PaymentChargeRequest(tenant.BillingEmail, prorated,
                AuthorizationCode: subscription.PaymentMethodRef), cancellationToken);
            if (!string.Equals(charge.Status, "success", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("The upgrade payment was not successful.");
        }
        SubscriptionStateMachine.ActivatePaidPlan(subscription, target.Id, request.BillingCycle, DateTime.UtcNow);
        subscription.Plan = target;
        await context.SaveChangesAsync(cancellationToken);
        return SubscriptionCommands.ToDto(subscription);
    }
}

public sealed class DowngradeSubscriptionCommandHandler(IAppDbContext context)
    : IRequestHandler<DowngradeSubscriptionCommand, BillingSubscriptionDto>
{
    public async Task<BillingSubscriptionDto> Handle(DowngradeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        var target = await context.PlanDefinitions.SingleOrDefaultAsync(plan => plan.Id == request.PlanDefinitionId && plan.IsActive, cancellationToken)
            ?? throw new NotFoundException("Target plan not found.");
        var exceeds = (target.MaxProducts is int products &&
                       await BillingUsageReader.CurrentAsync(context, UsageMetric.Products, subscription.TenantId, cancellationToken) > products) ||
                      (target.MaxLocations is int locations &&
                       await BillingUsageReader.CurrentAsync(context, UsageMetric.Locations, subscription.TenantId, cancellationToken) > locations) ||
                      (target.MaxUsers is int users &&
                       await BillingUsageReader.CurrentAsync(context, UsageMetric.Users, subscription.TenantId, cancellationToken) > users) ||
                      (target.MaxRegisters is int registers &&
                       await BillingUsageReader.CurrentAsync(context, UsageMetric.Registers, subscription.TenantId, cancellationToken) > registers);
        if (exceeds && !request.AcknowledgeOverLimit)
            throw new ConflictException("The tenant exceeds one or more limits of the selected plan; set acknowledgeOverLimit=true to schedule the downgrade.");
        subscription.PendingPlanDefinitionId = target.Id;
        await context.SaveChangesAsync(cancellationToken);
        return SubscriptionCommands.ToDto(subscription);
    }
}

public sealed class CancelSubscriptionCommandHandler(IAppDbContext context) : IRequestHandler<CancelSubscriptionCommand, BillingSubscriptionDto>
{
    public async Task<BillingSubscriptionDto> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        SubscriptionStateMachine.CancelAtPeriodEnd(subscription, DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        return SubscriptionCommands.ToDto(subscription);
    }
}

public sealed class ReactivateSubscriptionCommandHandler(IAppDbContext context) : IRequestHandler<ReactivateSubscriptionCommand, BillingSubscriptionDto>
{
    public async Task<BillingSubscriptionDto> Handle(ReactivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        try { SubscriptionStateMachine.Reactivate(subscription, DateTime.UtcNow); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        await context.SaveChangesAsync(cancellationToken);
        return SubscriptionCommands.ToDto(subscription);
    }
}
