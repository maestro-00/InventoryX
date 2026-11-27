using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

public sealed class StartRecoveryCommandHandler(
    IAppDbContext context, IRequestHandler<CreatePaymentIntentCommand, CreatePaymentIntentDto> paymentIntent)
    : IRequestHandler<StartRecoveryCommand, CreatePaymentIntentDto>
{
    public async Task<CreatePaymentIntentDto> Handle(StartRecoveryCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Subscriptions.Include(s => s.Plan).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Subscription not found.");
        if (subscription.Status != SubscriptionStatus.ReadOnly && subscription.Status != SubscriptionStatus.PastDue)
            throw new ConflictException("There is no outstanding recovery payment.");
        var plan = subscription.Plan ?? throw new NotFoundException("Subscription plan not found.");
        var amount = subscription.BillingCycle == BillingCycle.Annual ? plan.AnnualPrice : plan.MonthlyPrice;
        var quote = new BillingQuote
        {
            TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
            TargetPlanDefinitionId = subscription.PlanDefinitionId, TargetCycle = subscription.BillingCycle,
            Purpose = PaymentIntentPurpose.Recovery, PriceVersion = $"recovery:{subscription.Id:N}:{subscription.CurrentPeriodStart:O}",
            BaseAmount = amount, TotalAmount = amount, Currency = "GHS", EffectiveAt = DateTime.UtcNow,
            NextRenewalAt = subscription.CurrentPeriodEnd, NextRenewalTotal = amount, ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };
        context.BillingQuotes.Add(quote);
        await context.SaveChangesAsync(cancellationToken);
        return await paymentIntent.Handle(new CreatePaymentIntentCommand(quote.Id, request.MethodKind,
            request.Provider, request.PhoneNumber, request.IdempotencyKey), cancellationToken);
    }
}
