using InventoryX.Application.Commands.RequestHandlers.Purchasing;
using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Purchasing;

public sealed class GetPurchaseOrdersQueryHandler(IAppDbContext context) : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    public async Task<PagedResult<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = context.PurchaseOrders.AsNoTracking().Include(order => order.Lines).AsQueryable();
        if (request.Status is not null) query = query.Where(order => order.Status == request.Status);
        if (request.SupplierId is not null) query = query.Where(order => order.SupplierId == request.SupplierId);
        if (request.Overdue) query = query.Where(order => order.RequiredBy < DateTime.UtcNow && order.Status != Domain.Models.Purchasing.PurchaseOrderStatus.Closed && order.Status != Domain.Models.Purchasing.PurchaseOrderStatus.Cancelled);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(order => order.CreatedAt).Skip((Math.Max(1, request.Page) - 1) * Math.Clamp(request.PageSize, 1, 100)).Take(Math.Clamp(request.PageSize, 1, 100)).ToListAsync(cancellationToken);
        return PagedResult<PurchaseOrderDto>.Create(items.Select(PurchaseOrderCommandHandler.Map).ToList(), Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100), total);
    }
}
