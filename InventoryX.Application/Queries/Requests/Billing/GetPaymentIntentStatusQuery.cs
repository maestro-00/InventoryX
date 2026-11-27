using MediatR;

namespace InventoryX.Application.Queries.Requests.Billing;

/// <summary>Safe, server-confirmed status for one tenant-owned payment intent.</summary>
public sealed record PaymentIntentStatusDto(Guid IntentId, string Status, int PollAfterSeconds);

public sealed record GetPaymentIntentStatusQuery(Guid Id) : IRequest<PaymentIntentStatusDto>;
