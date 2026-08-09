using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Billing;

public sealed class GetBillingInvoicesQueryHandler(IAppDbContext context) : IRequestHandler<GetBillingInvoicesQuery, List<BillingInvoiceDto>>
{
    public async Task<List<BillingInvoiceDto>> Handle(GetBillingInvoicesQuery request, CancellationToken cancellationToken) =>
        await context.BillingInvoices.AsNoTracking().OrderByDescending(item => item.CreatedAt).Select(item =>
            new BillingInvoiceDto(item.Id, item.Number, item.Amount, item.TaxAmount, item.Currency, item.Status.ToString(), item.CreatedAt, item.EmailedTo))
            .ToListAsync(cancellationToken);
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
