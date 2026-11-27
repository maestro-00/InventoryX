using System.Security.Cryptography;
using System.Text;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Options;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

public sealed class InitializePaymentMethodCommandHandler(
    IAppDbContext context, IPaymentGateway paymentGateway, ITenantContext tenantContext,
    IBillingSecretProtector secretProtector, IOptions<PaystackOptions> paystackOptions)
    : IRequestHandler<InitializePaymentMethodCommand, InitializePaymentMethodResult>
{
    public const decimal VerificationAmount = 1m;

    public async Task<InitializePaymentMethodResult> Handle(InitializePaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is null || tenantContext.Role != "Owner")
            throw new UnauthorizedAccessException("Only the business owner can replace a payment method.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 200)
            throw new ConflictException("A valid Idempotency-Key is required.");
        var channel = request.Channel.Trim().ToLowerInvariant();
        if (channel is not ("card" or "mobile_money"))
            throw new FluentValidation.ValidationException("Channel must be card or mobile_money.");
        if (channel == "mobile_money" &&
            (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.Msisdn)))
            throw new FluentValidation.ValidationException("Mobile money requires provider and msisdn.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{channel}|{request.Provider}|{request.Msisdn}")));
        var prior = await context.PaymentIntents.SingleOrDefaultAsync(intent =>
            intent.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (prior is not null)
        {
            if (prior.Purpose != PaymentIntentPurpose.MethodVerification || prior.RequestHash != hash)
                throw new ConflictException("The Idempotency-Key was already used for another billing action.");
            var priorAttempt = await context.PaymentAttempts.SingleAsync(attempt =>
                attempt.PaymentIntentId == prior.Id, cancellationToken);
            return ToDto(prior, priorAttempt);
        }
        string? callbackUrl = null;
        if (channel == "card")
        {
            callbackUrl = paystackOptions.Value.CallbackUrl;
            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttps && !(parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback)) ||
                parsed.AbsolutePath.TrimEnd('/') != "/settings/billing")
                throw new FluentValidation.ValidationException("Paystack:CallbackUrl must be the owner billing return URL.");
            callbackUrl = parsed.AbsoluteUri;
        }
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        var tenant = await context.Tenants.SingleOrDefaultAsync(item => item.Id == subscription.TenantId,
            cancellationToken) ?? throw new NotFoundException("Tenant not found.");
        if (string.IsNullOrWhiteSpace(tenant.BillingEmail))
            throw new FluentValidation.ValidationException("A billing email is required before adding a payment method.");
        if (await context.PaymentIntents.AnyAsync(intent => intent.SubscriptionId == subscription.Id &&
            intent.Purpose == PaymentIntentPurpose.MethodVerification &&
            intent.Status == PaymentIntentStatus.Pending, cancellationToken))
            throw new ConflictException("A payment method verification is already pending.");

        var method = new PaymentMethod
        {
            TenantId = subscription.TenantId,
            Kind = channel == "card" ? PaymentMethodKind.Card : PaymentMethodKind.MobileMoney,
            RenewalMode = channel == "card" ? PaymentRenewalMode.Automatic : PaymentRenewalMode.OwnerApproval,
            Provider = channel == "card" ? "paystack" : request.Provider!.Trim().ToLowerInvariant(),
            MaskedIdentifier = channel == "card" ? "Card awaiting verification" :
                $"***{request.Msisdn![Math.Max(0, request.Msisdn!.Length - 4)..]}",
            EncryptedPhoneNumber = channel == "mobile_money"
                ? secretProtector.Protect(subscription.TenantId, request.Msisdn!) : null,
        };
        var intent = new PaymentIntent
        {
            TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
            Purpose = PaymentIntentPurpose.MethodVerification,
            BaseAmount = VerificationAmount, TotalAmount = VerificationAmount,
            Currency = tenant.Currency, IdempotencyKey = request.IdempotencyKey,
            RequestHash = hash,
        };
        var attempt = new PaymentAttempt
        {
            TenantId = subscription.TenantId, PaymentIntentId = intent.Id,
            PaymentMethodId = method.Id, Reference = $"method-{Guid.NewGuid():N}",
            Amount = VerificationAmount, Currency = tenant.Currency, InitiatedAt = DateTime.UtcNow,
        };
        context.PaymentMethods.Add(method);
        context.PaymentIntents.Add(intent);
        context.PaymentAttempts.Add(attempt);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            if (channel == "card")
            {
                var initialized = await paymentGateway.InitializeAuthorizationAsync(new PaymentInitializationRequest(
                    tenant.BillingEmail, VerificationAmount, tenant.Currency, "card", attempt.Reference,
                    callbackUrl), cancellationToken);
                if (initialized.Reference != attempt.Reference)
                    throw new InvalidOperationException("Paystack returned an unexpected method reference.");
                attempt.AuthorizationUrl = initialized.AuthorizationUrl;
            }
            else
            {
                var charged = await paymentGateway.ChargeAsync(new PaymentChargeRequest(
                    tenant.BillingEmail, VerificationAmount, tenant.Currency,
                    MobileMoneyProvider: request.Provider, Msisdn: request.Msisdn,
                    Reference: attempt.Reference), cancellationToken);
                if (charged.Reference != attempt.Reference)
                    throw new InvalidOperationException("Paystack returned an unexpected method reference.");
                attempt.ApprovalMessage = charged.DisplayText;
                if (charged.Status is "pay_offline" or "pending")
                    attempt.Status = PaymentAttemptStatus.AwaitingApproval;
                else if (charged.Status == "failed")
                {
                    attempt.MarkFailed(DateTime.UtcNow, "provider_failed");
                    intent.MarkFailed();
                    method.Invalidate();
                }
                else attempt.MarkUnknown();
            }
            await context.SaveChangesAsync(cancellationToken);
            return ToDto(intent, attempt);
        }
        catch
        {
            attempt.MarkUnknown();
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static InitializePaymentMethodResult ToDto(PaymentIntent intent, PaymentAttempt attempt) =>
        new(intent.Id, attempt.PaymentMethodId!.Value, attempt.Reference,
            attempt.Status switch
            {
                PaymentAttemptStatus.AwaitingApproval => "awaiting_approval",
                PaymentAttemptStatus.Succeeded => "succeeded",
                PaymentAttemptStatus.Failed => "failed",
                _ => "pending",
            }, intent.TotalAmount, intent.Currency,
            attempt.AuthorizationUrl, attempt.ApprovalMessage, 5);
}
