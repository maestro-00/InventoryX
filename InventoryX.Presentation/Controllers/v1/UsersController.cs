using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Users;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/users")]
[Authorize]
[Tags("Users")]
public sealed class UsersController(
    ISender sender,
    UserManager<User> userManager,
    IAppDbContext context,
    IPosAccess posAccess) : ApiControllerBase
{
    public sealed record SetPinRequest(string Pin);
    public sealed record InviteUserRequest(string Email, Guid? RoleId, string? LocationScope);
    public sealed record AcceptInvitationRequest(string Token, string Password);
    public sealed record UpdateUserRequest(Guid? RoleId, string? LocationScope, UserStatus? Status);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> List(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpPost("invitations")]
    [ProducesResponseType(typeof(InviteUserResultDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InviteUserResultDto>> Invite(
        InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        await posAccess.RequireAsync(Permission.ManageUsers, cancellationToken);
        var tenantId = TenantId ?? throw new UnauthorizedAccessException();
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            TenantId = tenantId,
            RoleId = request.RoleId,
            LocationScope = request.LocationScope,
            Status = UserStatus.Invited,
        };
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded) return ValidationProblem(new ValidationProblemDetails(result.Errors
            .GroupBy(item => item.Code).ToDictionary(group => group.Key, group => group.Select(item => item.Description).ToArray())));
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return CreatedAtAction(nameof(List), new InviteUserResultDto(user.Id, token));
    }

    [HttpPost("invitations/{id}/accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Accept(string id, AcceptInvitationRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null || user.Status != UserStatus.Invited) return NotFound();
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);
        user.Status = UserStatus.Active;
        await userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Owner,Administrator")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserListItemDto>> Update(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUserAccessCommand
        {
            UserId = id,
            RoleId = request.RoleId,
            LocationScope = request.LocationScope,
            Status = request.Status,
            ExpectedConcurrencyStamp = ParseIfMatchOpaque(),
        }, cancellationToken);
        SetETag(result.ConcurrencyStamp);
        return Ok(result);
    }

    [HttpGet("/api/v1/roles")]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoleDto>>> Roles(CancellationToken cancellationToken)
    {
        await posAccess.RequireAsync(Permission.ManageUsers, cancellationToken);
        return Ok(await context.AppRoles.AsNoTracking().OrderBy(item => item.Name).Select(item => new RoleDto(
            item.Id, item.Name, item.Permissions.ToString(), item.MaxDiscountPercent, item.MaxUnauthorizedRefundAmount))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("/api/v1/audit-log")]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<ActionResult<PagedResult<object>>> AuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = context.AuditLogEntries.AsNoTracking().OrderByDescending(item => item.OccurredAt);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(PagedResult<object>.Create(items.Cast<object>().ToList(), page, pageSize, totalCount));
    }

    [HttpPut("{userId}/pin")]
    public async Task<IActionResult> SetPin(string userId, SetPinRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetRegisterPinCommand { UserId = userId, Pin = request.Pin }, cancellationToken);
        return NoContent();
    }
}
