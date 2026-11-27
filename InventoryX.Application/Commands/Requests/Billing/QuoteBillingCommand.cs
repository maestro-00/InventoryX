using InventoryX.Application.Behaviors;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Billing;

public sealed record QuoteBillingCommand(
    Guid TargetPlanId, BillingCycle BillingCycle, PaymentIntentPurpose Purpose,
    string? IdempotencyKey = null) : IRequest<BillingQuoteDto>, IBillingIdempotentRequest;

public sealed record BillingQuoteDto(
    Guid QuoteId, DateTime ExpiresAt, string Currency,
    decimal BaseAmount, decimal TaxAmount, decimal TotalAmount, decimal ProrationCredit,
    DateTime EffectiveAt, DateTime NextRenewalAt, decimal NextRenewalTotal,
    IReadOnlyList<string> ExceededLimits, bool RequiresAcknowledgement);
