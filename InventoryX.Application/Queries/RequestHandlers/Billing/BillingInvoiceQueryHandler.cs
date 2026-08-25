using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Billing;

public sealed class GetBillingInvoicesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetBillingInvoicesQuery, PagedResult<BillingInvoiceDto>>
{
    public async Task<PagedResult<BillingInvoiceDto>> Handle(GetBillingInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = context.BillingInvoices.AsNoTracking().OrderByDescending(item => item.CreatedAt);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(item => new BillingInvoiceDto(
                item.Id, item.Number, item.Amount, item.TaxAmount, item.Currency,
                item.Status.ToString(), item.CreatedAt, item.EmailedTo))
            .ToListAsync(cancellationToken);
        return PagedResult<BillingInvoiceDto>.Create(items, request.Page, request.PageSize, total);
    }
}

public sealed class GetBillingInvoicePdfQueryHandler(IAppDbContext context) : IRequestHandler<GetBillingInvoicePdfQuery, BillingInvoicePdfDto>
{
    public async Task<BillingInvoicePdfDto> Handle(GetBillingInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await context.BillingInvoices.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Billing invoice not found.");
        return new BillingInvoicePdfDto(invoice.Number, invoice.PdfContent ?? throw new NotFoundException("Invoice PDF not found."));
    }
}
