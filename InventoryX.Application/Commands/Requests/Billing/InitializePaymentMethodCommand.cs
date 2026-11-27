using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Billing;

public sealed record InitializePaymentMethodResult(Guid IntentId, Guid PendingMethodId,
    string Reference, string Status, decimal Amount, string Currency,
    string? AuthorizationUrl, string? ApprovalMessage, int PollAfterSeconds);

public sealed class InitializePaymentMethodCommand : IRequest<InitializePaymentMethodResult>, IReadOnlyWriteExemptCommand, IAuditedCommand
{
    public string Channel { get; init; } = "card";
    public string? Provider { get; init; }
    public string? Msisdn { get; init; }
    public string? IdempotencyKey { get; init; }
    public string AuditAction => "billing.payment-method.initialize";
    public string AuditEntityType => "Subscription";
    public string AuditEntityId => "current";
}
