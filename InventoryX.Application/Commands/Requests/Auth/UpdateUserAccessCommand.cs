using InventoryX.Application.Behaviors;
using InventoryX.Domain.Models;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Auth;

public sealed class UpdateUserAccessCommand : IRequest<bool>, ITenantWriteCommand, IAuditedCommand
{
    public required string UserId { get; init; }
    public Guid? RoleId { get; init; }
    public string? LocationScope { get; init; }
    public UserStatus? Status { get; init; }

    public string AuditAction => "user.permissions.update";
    public string AuditEntityType => "User";
    public string AuditEntityId => UserId;
}
