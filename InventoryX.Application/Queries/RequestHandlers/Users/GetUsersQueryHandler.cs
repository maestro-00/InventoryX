using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Users;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Users;

public sealed class GetUsersQueryHandler(IAppDbContext context, ITenantContext tenantContext, IPosAccess posAccess)
    : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        await posAccess.RequireAsync(Permission.ManageUsers, cancellationToken);
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var query = context.Users.AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.Email);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(item => new UserListItemDto(
                item.Id, item.Email, item.Name, item.RoleId, item.LocationScope,
                item.Status, item.IsOwner, item.ConcurrencyStamp))
            .ToListAsync(cancellationToken);
        return PagedResult<UserListItemDto>.Create(items, request.Page, request.PageSize, total);
    }
}
