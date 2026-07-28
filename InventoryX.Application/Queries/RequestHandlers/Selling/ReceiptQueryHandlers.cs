using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Selling;

public sealed class GetSaleReceiptQueryHandler(IAppDbContext context) : IRequestHandler<GetSaleReceiptQuery, ReceiptDto>
{
    public async Task<ReceiptDto> Handle(GetSaleReceiptQuery request, CancellationToken cancellationToken)
    {
        var receipt = await context.Receipts.SingleOrDefaultAsync(r => r.SaleId == request.SaleId, cancellationToken)
            ?? throw new NotFoundException("Receipt not found.");
        return new ReceiptDto(receipt.Id, receipt.SaleId, receipt.Number, receipt.PayloadJson, receipt.CreatedAt);
    }
}

public sealed class GetReceiptTemplateQueryHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetReceiptTemplateQuery, ReceiptTemplateDto>
{
    public async Task<ReceiptTemplateDto> Handle(GetReceiptTemplateQuery request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.SingleAsync(t => t.Id == tenantContext.TenantId, cancellationToken);
        return new ReceiptTemplateDto(tenant.ReceiptTemplate ?? "{}");
    }
}
