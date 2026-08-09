using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Sync;

public sealed class GetSyncConflictsQueryHandler(IAppDbContext context) : IRequestHandler<GetSyncConflictsQuery, List<SaleDto>>
{
    public async Task<List<SaleDto>> Handle(GetSyncConflictsQuery request, CancellationToken cancellationToken) =>
        (await context.Sales.AsNoTracking().Include(s => s.Lines).Include(s => s.Payments)
            .Where(s => s.StockConflictFlag).OrderBy(s => s.OccurredAt).ToListAsync(cancellationToken))
        .Select(SaleMapping.ToDto).ToList();
}
