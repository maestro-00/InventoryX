using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Selling;

public record GetSaleReceiptQuery(Guid SaleId) : IRequest<ReceiptDto>;
public record GetReceiptTemplateQuery : IRequest<ReceiptTemplateDto>;
