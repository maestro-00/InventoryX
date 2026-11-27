using System.Security.Cryptography;
using System.Text;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Options;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

public sealed class CreatePaymentIntentCommandHandler(
    IAppDbContext context, ITenantContext tenantContext, IPaymentGateway gateway,
    IOptions<PaystackOptions> paystackOptions, IBillingSecretProtector secretProtector)
    : IRequestHandler<CreatePaymentIntentCommand, CreatePaymentIntentDto>
{
    public async Task<CreatePaymentIntentDto> Handle(CreatePaymentIntentCommand request, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is null || tenantContext.Role != "Owner")
            throw new UnauthorizedAccessException("Only the business owner can start a payment.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 200)
            throw new ConflictException("A valid Idempotency-Key is required.");
        var methodKind = request.MethodKind.Trim().ToLowerInvariant();
        if (methodKind is not ("card" or "mobile_money"))
            throw new FluentValidation.ValidationException("Choose card or mobile_money.");
        if (methodKind == "mobile_money" &&
            (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.PhoneNumber)))
            throw new FluentValidation.ValidationException("Mobile money requires provider and phone number.");
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{request.QuoteId:N}|{methodKind}|{request.Provider}|{request.PhoneNumber}")));
        var existing = await context.PaymentIntents.SingleOrDefaultAsync(i => i.IdempotencyKey == request.IdempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
                throw new ConflictException("The Idempotency-Key was already used for another purchase.");
            var priorAttempt = await context.PaymentAttempts.SingleAsync(a => a.PaymentIntentId == existing.Id,
                cancellationToken);
            return ToDto(existing.Id, priorAttempt);
        }

        var quote = await context.BillingQuotes.SingleOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken)
            ?? throw new NotFoundException("Quote not found.");
        if (quote.ExpiresAt <= DateTime.UtcNow || quote.Purpose is not (PaymentIntentPurpose.Initial or PaymentIntentPurpose.Recovery or PaymentIntentPurpose.Upgrade))
            throw new ConflictException("The quote has expired or is no longer valid.");
        string? callbackUrl = null;
        if (methodKind == "card" && quote.Purpose != PaymentIntentPurpose.Upgrade)
        {
            callbackUrl = paystackOptions.Value.CallbackUrl;
            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callback) ||
                (callback.Scheme != Uri.UriSchemeHttps && !(callback.Scheme == Uri.UriSchemeHttp && callback.IsLoopback)) ||
                callback.AbsolutePath.TrimEnd('/') != "/settings/billing" ||
                !string.IsNullOrEmpty(callback.UserInfo))
                throw new FluentValidation.ValidationException(
                    "Paystack:CallbackUrl must be the absolute owner billing return URL.");
            callbackUrl = callback.AbsoluteUri;
        }
        var plan = await context.PlanDefinitions.SingleOrDefaultAsync(p => p.Id == quote.TargetPlanDefinitionId && p.IsActive,
            cancellationToken) ?? throw new ConflictException("The selected plan is no longer published.");
        if (quote.Purpose != PaymentIntentPurpose.Recovery &&
            quote.PriceVersion != QuoteBillingCommandHandler.PriceVersion(plan, quote.TargetCycle))
            throw new ConflictException("The plan price changed; request a new quote.");
        var subscription = await context.Subscriptions.Include(s => s.Plan)
            .SingleAsync(s => s.Id == quote.SubscriptionId, cancellationToken);
        var tenant = await context.Tenants.SingleAsync(t => t.Id == tenantContext.TenantId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenant.BillingEmail))
            throw new FluentValidation.ValidationException("A billing email is required before payment.");
        if (await context.PaymentIntents.AnyAsync(i => i.SubscriptionId == subscription.Id &&
            i.Status == PaymentIntentStatus.Pending, cancellationToken))
            throw new ConflictException("A payment is already pending; check its status before retrying.");

        var start = quote.Purpose == PaymentIntentPurpose.Recovery ? subscription.CurrentPeriodStart : DateTime.UtcNow;
        var end = BillingPeriodService.Boundary(new DateTimeOffset(start), quote.TargetCycle, 1).UtcDateTime;
        BillingPeriod period;
        if (quote.Purpose == PaymentIntentPurpose.Upgrade)
        {
            period = await context.BillingPeriods.SingleOrDefaultAsync(p => p.SubscriptionId == subscription.Id &&
                p.StartAt == subscription.CurrentPeriodStart && p.EndAt == subscription.CurrentPeriodEnd, cancellationToken)
                ?? new BillingPeriod
                {
                    TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
                    StartAt = subscription.CurrentPeriodStart, EndAt = subscription.CurrentPeriodEnd,
                    Cycle = subscription.BillingCycle, PlanDefinitionId = subscription.PlanDefinitionId,
                    PriceVersion = $"existing:{subscription.PlanDefinitionId:N}", BaseAmount = currentPrice(subscription),
                    TotalAmount = currentPrice(subscription), Currency = quote.Currency,
                };
            if (period.Id == Guid.Empty) { period.MarkPaid(DateTime.UtcNow); context.BillingPeriods.Add(period); }
        }
        else
        {
            period = new BillingPeriod
            {
                TenantId = subscription.TenantId, SubscriptionId = subscription.Id,
                StartAt = start, EndAt = end, Cycle = quote.TargetCycle,
                PlanDefinitionId = quote.TargetPlanDefinitionId, PriceVersion = quote.PriceVersion,
                BaseAmount = quote.BaseAmount, TaxAmount = quote.TaxAmount,
                TotalAmount = quote.TotalAmount, Currency = quote.Currency,
            };
            context.BillingPeriods.Add(period);
        }
        var intent = new PaymentIntent
        {
            TenantId = subscription.TenantId, SubscriptionId = subscription.Id, BillingPeriodId = period.Id,
            Purpose = quote.Purpose, TargetPlanDefinitionId = quote.TargetPlanDefinitionId,
            TargetCycle = quote.TargetCycle, BaseAmount = quote.BaseAmount, TaxAmount = quote.TaxAmount,
            TotalAmount = quote.TotalAmount, Currency = quote.Currency,
            QuoteExpiresAt = quote.ExpiresAt, ExpectedPeriodStart = start, ExpectedPeriodEnd = end,
            IdempotencyKey = request.IdempotencyKey, RequestHash = requestHash,
        };
        var attempt = new PaymentAttempt
        {
            TenantId = subscription.TenantId, PaymentIntentId = intent.Id,
            Reference = $"bill-{Guid.NewGuid():N}", Amount = quote.TotalAmount,
            Currency = quote.Currency, InitiatedAt = DateTime.UtcNow,
        };
        var method = quote.Purpose == PaymentIntentPurpose.Upgrade
            ? (subscription.CurrentPaymentMethodId is { } currentMethodId
                ? await context.PaymentMethods.SingleOrDefaultAsync(m => m.Id == currentMethodId &&
                    m.Status == PaymentMethodStatus.Active, cancellationToken)
                : null)
            : new PaymentMethod
            {
                TenantId = subscription.TenantId,
                Kind = methodKind == "card" ? PaymentMethodKind.Card : PaymentMethodKind.MobileMoney,
                RenewalMode = methodKind == "card" ? PaymentRenewalMode.Automatic : PaymentRenewalMode.OwnerApproval,
                Provider = methodKind == "card" ? "paystack" : request.Provider!.Trim().ToLowerInvariant(),
                MaskedIdentifier = methodKind == "card" ? "Card awaiting verification" :
                    $"***{request.PhoneNumber![Math.Max(0, request.PhoneNumber!.Length - 4)..]}",
                EncryptedPhoneNumber = methodKind == "mobile_money"
                    ? secretProtector.Protect(subscription.TenantId, request.PhoneNumber!) : null,
            };
        if (method is null) throw new ConflictException("Add an active payment method before upgrading.");
        attempt.PaymentMethodId = method.Id;
        context.PaymentIntents.Add(intent);
        if (quote.Purpose != PaymentIntentPurpose.Upgrade) context.PaymentMethods.Add(method);
        context.PaymentAttempts.Add(attempt);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var winner = await context.PaymentIntents.AsNoTracking().SingleOrDefaultAsync(
                i => i.IdempotencyKey == request.IdempotencyKey, cancellationToken);
            if (winner is null) throw;
            if (winner.RequestHash != requestHash)
                throw new ConflictException("The Idempotency-Key was already used for another purchase.");
            var winnerAttempt = await context.PaymentAttempts.AsNoTracking().SingleAsync(
                a => a.PaymentIntentId == winner.Id, cancellationToken);
            return ToDto(winner.Id, winnerAttempt);
        }

        try
        {
            if (quote.Purpose == PaymentIntentPurpose.Upgrade)
            {
                var charged = await gateway.ChargeAsync(new PaymentChargeRequest(
                    tenant.BillingEmail, quote.TotalAmount, quote.Currency,
                    AuthorizationCode: method.Kind == PaymentMethodKind.Card && method.EncryptedAuthorizationCode is not null
                        ? secretProtector.Unprotect(subscription.TenantId, method.EncryptedAuthorizationCode) : null,
                    MobileMoneyProvider: method.Kind == PaymentMethodKind.MobileMoney ? method.Provider : null,
                    Msisdn: method.Kind == PaymentMethodKind.MobileMoney && method.EncryptedPhoneNumber is not null
                        ? secretProtector.Unprotect(subscription.TenantId, method.EncryptedPhoneNumber) : null,
                    Reference: attempt.Reference), cancellationToken);
                if (charged.Reference != attempt.Reference)
                    throw new InvalidOperationException("Paystack returned an unexpected payment reference.");
                attempt.ApprovalMessage = charged.DisplayText;
                if (charged.Status is "pay_offline" or "pending") attempt.Status = PaymentAttemptStatus.AwaitingApproval;
                else if (charged.Status == "failed") { attempt.MarkFailed(DateTime.UtcNow, "provider_failed"); intent.MarkFailed(); }
                else attempt.MarkUnknown();
            }
            else if (methodKind == "card")
            {
                var initialized = await gateway.InitializeAuthorizationAsync(new PaymentInitializationRequest(
                    tenant.BillingEmail, quote.TotalAmount, quote.Currency, "card", attempt.Reference,
                    callbackUrl), cancellationToken);
                if (initialized.Reference != attempt.Reference)
                    throw new InvalidOperationException("Paystack returned an unexpected payment reference.");
                attempt.AuthorizationUrl = initialized.AuthorizationUrl;
            }
            else
            {
                var charged = await gateway.ChargeAsync(new PaymentChargeRequest(
                    tenant.BillingEmail, quote.TotalAmount, quote.Currency,
                    MobileMoneyProvider: request.Provider, Msisdn: request.PhoneNumber,
                    Reference: attempt.Reference), cancellationToken);
                if (charged.Reference != attempt.Reference)
                    throw new InvalidOperationException("Paystack returned an unexpected payment reference.");
                attempt.ApprovalMessage = charged.DisplayText;
                if (charged.Status is "pay_offline" or "pending")
                    attempt.Status = PaymentAttemptStatus.AwaitingApproval;
                else if (charged.Status == "failed")
                {
                    attempt.MarkFailed(DateTime.UtcNow, "provider_failed");
                    intent.MarkFailed();
                    method.Invalidate();
                }
                else
                    attempt.MarkUnknown();
            }
            await context.SaveChangesAsync(cancellationToken);
            return ToDto(intent.Id, attempt);
        }
        catch
        {
            attempt.MarkUnknown();
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static decimal currentPrice(Subscription subscription) => subscription.Plan is null
        ? 0m : subscription.BillingCycle == BillingCycle.Annual ? subscription.Plan.AnnualPrice : subscription.Plan.MonthlyPrice;

    private static CreatePaymentIntentDto ToDto(Guid intentId, PaymentAttempt attempt) =>
        new(intentId, attempt.Status switch
            {
                PaymentAttemptStatus.AwaitingApproval => "awaiting_approval",
                PaymentAttemptStatus.Failed => "failed",
                PaymentAttemptStatus.Succeeded => "succeeded",
                _ => "pending",
            },
            attempt.AuthorizationUrl, attempt.ApprovalMessage, 5);
}
