using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Application.Repository;
using MediatR;

namespace InventoryX.Application.Queries.RequestHandlers.Inventory;

public sealed class GetStockCountQueryHandler(IAppDbContext context) : IRequestHandler<GetStockCountQuery, StockCountResult>
{
    public async Task<StockCountResult> Handle(GetStockCountQuery request, CancellationToken cancellationToken) =>
        StockCountMapping.ToResult(await UpdateStockCountLinesCommandHandler.LoadAsync(context, request.CountId, cancellationToken));
}
