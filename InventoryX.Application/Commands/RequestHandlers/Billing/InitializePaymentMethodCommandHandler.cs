using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

public sealed class InitializePaymentMethodCommandHandler(IAppDbContext context, IPaymentGateway paymentGateway)
    : IRequestHandler<InitializePaymentMethodCommand, PaymentInitializationResult>
{
    public async Task<PaymentInitializationResult> Handle(InitializePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        var tenant = await context.Tenants.SingleOrDefaultAsync(item => item.Id == subscription.TenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");
        if (string.IsNullOrWhiteSpace(tenant.BillingEmail))
            throw new FluentValidation.ValidationException("A billing email is required before adding a payment method.");
        var channel = request.Channel.Trim().ToLowerInvariant();
        if (channel is not ("card" or "mobile_money"))
            throw new FluentValidation.ValidationException("Channel must be card or mobile_money.");
        if (channel == "mobile_money" && (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.Msisdn)))
            throw new FluentValidation.ValidationException("Mobile money requires provider and msisdn.");
        var amount = subscription.BillingCycle == Domain.Models.Tenancy.BillingCycle.Annual
            ? subscription.Plan!.AnnualPrice : subscription.Plan!.MonthlyPrice;
        var result = await paymentGateway.InitializeAuthorizationAsync(new PaymentInitializationRequest(
            tenant.BillingEmail, Math.Max(amount, 1m), tenant.Currency, channel), cancellationToken);
        subscription.PaymentMethodKind = channel;
        subscription.PaymentProvider = request.Provider?.Trim().ToLowerInvariant();
        subscription.PaymentMethodRef = result.Reference;
        await context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
