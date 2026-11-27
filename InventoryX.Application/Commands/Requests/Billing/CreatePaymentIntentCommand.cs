using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Billing;

public sealed record CreatePaymentIntentCommand(
    Guid QuoteId, string MethodKind, string? Provider = null, string? PhoneNumber = null,
    string? IdempotencyKey = null)
    : IRequest<CreatePaymentIntentDto>, IReadOnlyWriteExemptCommand;

public sealed record CreatePaymentIntentDto(
    Guid IntentId, string Status, string? AuthorizationUrl,
    string? ApprovalMessage, int PollAfterSeconds);

public sealed record StartRecoveryCommand(string MethodKind, string? Provider = null, string? PhoneNumber = null,
    string? IdempotencyKey = null) : IRequest<CreatePaymentIntentDto>, IReadOnlyWriteExemptCommand;
