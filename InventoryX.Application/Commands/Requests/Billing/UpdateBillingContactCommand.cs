using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Billing;

public sealed record UpdateBillingContactCommand(string BillingEmail, string? TaxNumber) : IRequest, ITenantWriteCommand, IAuditedCommand
{
    public string AuditAction => "billing.contact.update";
    public string AuditEntityType => "Tenant";
    public string AuditEntityId => "self";
}
