using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Billing;

public record BillingPlanDto(
    Guid Id, string Tier, string Name, decimal MonthlyPrice, decimal AnnualPrice,
    int? MaxLocations, int? MaxUsers, int? MaxProducts, int? MaxRegisters,
    int? MonthlySaleCap, IReadOnlyDictionary<string, bool> Features);

public record UsageVsLimitDto(string Metric, int Current, int? Limit);

public record BillingSubscriptionDto(
    Guid Id, string Plan, string Status, string BillingCycle,
    DateTime CurrentPeriodStart, DateTime CurrentPeriodEnd, DateTime? TrialEndsAt,
    DateTime? GraceExpiresAt, DateTime? CancelledAt, DateTime? PurgeAt,
    IReadOnlyList<UsageVsLimitDto> Usage);

public record GetBillingPlansQuery : IRequest<List<BillingPlanDto>>;
public record GetCurrentBillingSubscriptionQuery : IRequest<BillingSubscriptionDto>;
