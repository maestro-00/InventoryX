using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.DTOs.Common;
using InventoryX.Domain.Models.Purchasing;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Purchasing;

public sealed record GetPurchaseOrdersQuery(PurchaseOrderStatus? Status = null, Guid? SupplierId = null, bool Overdue = false, int Page = 1, int PageSize = 50) : IRequest<PagedResult<PurchaseOrderDto>>;
public sealed record GetPurchaseOrderPdfQuery(Guid Id) : IRequest<Services.IServices.PurchaseOrderDocument>;
