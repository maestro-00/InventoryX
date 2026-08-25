using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.DTOs.Users;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Auth;

public sealed class UpdateUserAccessCommandHandler(IAppDbContext context, ITenantContext tenantContext, IPosAccess posAccess)
    : IRequestHandler<UpdateUserAccessCommand, UserListItemDto>
{
    public async Task<UserListItemDto> Handle(UpdateUserAccessCommand request, CancellationToken cancellationToken)
    {
        await posAccess.RequireAsync(Permission.ManageUsers, cancellationToken);
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var user = await context.Users.SingleOrDefaultAsync(
                item => item.Id == request.UserId && item.TenantId == tenantId,
                cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!string.IsNullOrEmpty(request.ExpectedConcurrencyStamp)
            && !string.Equals(user.ConcurrencyStamp, request.ExpectedConcurrencyStamp, StringComparison.Ordinal))
            throw new ConflictException("The resource was modified by another request. Reload and retry with the latest ETag.");

        if (user.IsOwner && request.Status == Domain.Models.UserStatus.Deactivated)
            throw new ConflictException("The tenant owner cannot be deactivated.");

        user.RoleId = request.RoleId ?? user.RoleId;
        user.LocationScope = request.LocationScope ?? user.LocationScope;
        user.Status = request.Status ?? user.Status;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        await context.SaveChangesAsync(cancellationToken);
        return new UserListItemDto(
            user.Id, user.Email, user.Name, user.RoleId, user.LocationScope, user.Status, user.IsOwner, user.ConcurrencyStamp);
    }
}
