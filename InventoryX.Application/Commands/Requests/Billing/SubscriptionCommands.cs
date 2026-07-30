using InventoryX.Application.Behaviors;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Billing;

public sealed class UpgradeSubscriptionCommand : IRequest<BillingSubscriptionDto>, ITenantWriteCommand, IAuditedCommand
{
    public Guid PlanDefinitionId { get; init; }
    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;
    public string Email { get; init; } = string.Empty;
    public string AuditAction => "billing.subscription.upgrade";
    public string AuditEntityType => "Subscription";
    public string AuditEntityId => "current";
}

public sealed class DowngradeSubscriptionCommand : IRequest<BillingSubscriptionDto>, ITenantWriteCommand, IAuditedCommand
{
    public Guid PlanDefinitionId { get; init; }
    public bool AcknowledgeOverLimit { get; init; }
    public string AuditAction => "billing.subscription.downgrade";
    public string AuditEntityType => "Subscription";
    public string AuditEntityId => "current";
}

public sealed class CancelSubscriptionCommand : IRequest<BillingSubscriptionDto>, ITenantWriteCommand, IAuditedCommand
{
    public string AuditAction => "billing.subscription.cancel";
    public string AuditEntityType => "Subscription";
    public string AuditEntityId => "current";
}

public sealed class ReactivateSubscriptionCommand : IRequest<BillingSubscriptionDto>, ITenantWriteCommand, IAuditedCommand
{
    public string AuditAction => "billing.subscription.reactivate";
    public string AuditEntityType => "Subscription";
    public string AuditEntityId => "current";
}
