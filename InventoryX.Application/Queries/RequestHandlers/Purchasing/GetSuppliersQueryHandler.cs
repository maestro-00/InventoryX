using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Purchasing;

public sealed class GetSuppliersQueryHandler(IAppDbContext context)
    : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierDto>>
{
    public async Task<PagedResult<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Suppliers.AsNoTracking().OrderBy(item => item.Name);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(item => new SupplierDto(item.Id, item.Name, item.Email, item.Phone, item.LeadTimeDays, item.RowVersion))
            .ToListAsync(cancellationToken);
        return PagedResult<SupplierDto>.Create(items, request.Page, request.PageSize, total);
    }
}
