using InventoryX.Application.DTOs.Common;
using InventoryX.Domain.Models;
using MediatR;

namespace InventoryX.Application.DTOs.Users;

public sealed record UserListItemDto(
    string Id,
    string? Email,
    string? Name,
    Guid? RoleId,
    string? LocationScope,
    UserStatus Status,
    bool IsOwner,
    string? ConcurrencyStamp);

public sealed record RoleDto(
    Guid Id,
    string Name,
    string Permissions,
    decimal? MaxDiscountPercent,
    decimal? MaxUnauthorizedRefundAmount);

public sealed record InviteUserResultDto(string UserId, string? InviteToken);

public sealed record TwoFactorVerifyResultDto(bool Enabled);

public sealed record GetUsersQuery : PageRequest, IRequest<PagedResult<UserListItemDto>>;
