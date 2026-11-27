using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Billing;

public sealed record BillingPlanDto(
    Guid Id, string Tier, string Name, decimal MonthlyPrice, decimal AnnualPrice,
    int? MaxLocations, int? MaxUsers, int? MaxProducts, int? MaxRegisters,
    int? MonthlySaleCap, IReadOnlyDictionary<string, bool> Features,
    string Currency, decimal AnnualSavings, bool ContactSales);

public record UsageVsLimitDto(string Metric, int Current, int? Limit);

public record BillingSubscriptionDto(
    Guid Id, string Plan, string Status, string BillingCycle,
    DateTime CurrentPeriodStart, DateTime CurrentPeriodEnd, DateTime? TrialEndsAt,
    DateTime? GraceExpiresAt, DateTime? CancelledAt, DateTime? PurgeAt,
    IReadOnlyList<UsageVsLimitDto> Usage,
    DateTime? NextRenewalAt = null, decimal? NextRenewalAmount = null,
    BillingMethodSummaryDto? MethodSummary = null,
    bool CanPerformPaidMutations = true, string? ReadOnlyReason = null,
    Guid? ScheduledChangeId = null, string? ScheduledChangeKind = null,
    DateTime? ScheduledChangeEffectiveAt = null, decimal? OutstandingAmount = null,
    DateTime? LastChargeAttemptAt = null, int FailedChargeAttempts = 0,
    string? RetryStatus = null);

public sealed record BillingMethodSummaryDto(
    Guid Id, string Kind, string Provider, string MaskedIdentifier,
    string? Brand, int? ExpiryMonth, int? ExpiryYear, string RenewalMode);

public record GetBillingPlansQuery : IRequest<List<BillingPlanDto>>;
public record GetCurrentBillingSubscriptionQuery : IRequest<BillingSubscriptionDto>;

public sealed record BillingPaymentMethodDto(Guid Id, string Kind, string Status,
    string Provider, string MaskedIdentifier, string? Brand, int? ExpiryMonth,
    int? ExpiryYear, string RenewalMode);
public record GetBillingPaymentMethodsQuery : IRequest<List<BillingPaymentMethodDto>>;
