using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Inventory;

public sealed class GetAdjustmentReasonsQueryHandler(IAppDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetAdjustmentReasonsQuery, List<AdjustmentReasonDto>>
{
    public async Task<List<AdjustmentReasonDto>> Handle(GetAdjustmentReasonsQuery request, CancellationToken cancellationToken) =>
        await context.AdjustmentReasons
            .Where(r => r.TenantId == null || r.TenantId == tenantContext.TenantId)
            .OrderBy(r => r.Name)
            .Select(r => new AdjustmentReasonDto(r.Id, r.Code, r.Name, r.IsSystem))
            .ToListAsync(cancellationToken);
}
