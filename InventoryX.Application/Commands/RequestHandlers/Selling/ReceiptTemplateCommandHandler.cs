using System.Text.Json;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Selling;

public sealed class UpdateReceiptTemplateCommandHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<UpdateReceiptTemplateCommand, ReceiptTemplateDto>
{
    public async Task<ReceiptTemplateDto> Handle(UpdateReceiptTemplateCommand request, CancellationToken cancellationToken)
    {
        try { JsonDocument.Parse(request.TemplateJson); }
        catch (JsonException) { throw new FluentValidation.ValidationException("Receipt template must be valid JSON."); }
        var tenant = await context.Tenants.SingleAsync(t => t.Id == tenantContext.TenantId, cancellationToken);
        tenant.ReceiptTemplate = request.TemplateJson;
        await context.SaveChangesAsync(cancellationToken);
        return new ReceiptTemplateDto(tenant.ReceiptTemplate);
    }
}
