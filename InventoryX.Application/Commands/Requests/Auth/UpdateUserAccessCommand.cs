using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Users;
using InventoryX.Domain.Models;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Auth;

public sealed class UpdateUserAccessCommand : IRequest<UserListItemDto>, ITenantWriteCommand, IAuditedCommand
{
    public required string UserId { get; init; }
    public Guid? RoleId { get; init; }
    public string? LocationScope { get; init; }
    public UserStatus? Status { get; init; }
    /// <summary>Identity ConcurrencyStamp from If-Match (User has no SQL RowVersion).</summary>
    public string? ExpectedConcurrencyStamp { get; init; }

    public string AuditAction => "user.permissions.update";
    public string AuditEntityType => "User";
    public string AuditEntityId => UserId;
}
