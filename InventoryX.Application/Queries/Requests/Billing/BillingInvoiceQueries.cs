using InventoryX.Application.DTOs.Common;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Billing;

public sealed record BillingInvoiceDto(Guid Id, string Number, decimal Amount, decimal TaxAmount, string Currency,
    string Status, DateTime CreatedAt, string? EmailedTo);
public sealed record GetBillingInvoicesQuery : PageRequest, IRequest<PagedResult<BillingInvoiceDto>>;
public sealed record GetBillingInvoicePdfQuery(Guid Id) : IRequest<BillingInvoicePdfDto>;
public sealed record BillingInvoicePdfDto(string Number, string Content);
