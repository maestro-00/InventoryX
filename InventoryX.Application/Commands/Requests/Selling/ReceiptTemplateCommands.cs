using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling;

public record UpdateReceiptTemplateCommand(string TemplateJson) : IRequest<ReceiptTemplateDto>, IAuditedCommand
{
    public string AuditAction => "receipt-template.update";
    public string AuditEntityType => "Tenant";
    public string AuditEntityId => "self";
}
