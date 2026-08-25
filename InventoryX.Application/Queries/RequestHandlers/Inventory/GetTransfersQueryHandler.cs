using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Inventory;

public sealed class GetTransfersQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTransfersQuery, PagedResult<StockTransferDto>>
{
    public async Task<PagedResult<StockTransferDto>> Handle(GetTransfersQuery request, CancellationToken cancellationToken)
    {
        var query = context.StockTransfers.AsNoTracking().AsQueryable();
        if (request.Status is not null)
            query = query.Where(item => item.Status == request.Status);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(item => new StockTransferDto(
                item.Id,
                item.FromLocationId,
                item.ToLocationId,
                item.Status.ToString(),
                item.DiscrepancyReason,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<StockTransferDto>.Create(items, request.Page, request.PageSize, total);
    }
}
