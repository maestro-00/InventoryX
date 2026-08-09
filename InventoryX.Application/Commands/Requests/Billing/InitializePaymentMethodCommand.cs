using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Billing;

public sealed class InitializePaymentMethodCommand : IRequest<PaymentInitializationResult>, IReadOnlyWriteExemptCommand, IAuditedCommand
{
    public string Channel { get; init; } = "card";
    public string? Provider { get; init; }
    public string? Msisdn { get; init; }
    public string AuditAction => "billing.payment-method.initialize";
    public string AuditEntityType => "Subscription";
    public string AuditEntityId => "current";
}
