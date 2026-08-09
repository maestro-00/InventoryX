using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Auth;

public sealed class UpdateUserAccessCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateUserAccessCommand, bool>
{
    public async Task<bool> Handle(UpdateUserAccessCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.IsOwner && request.Status == Domain.Models.UserStatus.Deactivated)
            throw new ConflictException("The tenant owner cannot be deactivated.");

        user.RoleId = request.RoleId ?? user.RoleId;
        user.LocationScope = request.LocationScope ?? user.LocationScope;
        user.Status = request.Status ?? user.Status;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
