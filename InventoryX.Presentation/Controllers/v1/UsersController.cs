using InventoryX.Application.Commands.Requests.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(ISender sender, UserManager<User> userManager, IAppDbContext context) : ApiControllerBase
{
    public sealed record SetPinRequest(string Pin);
    public sealed record InviteUserRequest(string Email, Guid? RoleId, string? LocationScope);
    public sealed record AcceptInvitationRequest(string Token, string Password);
    public sealed record UpdateUserRequest(Guid? RoleId, string? LocationScope, UserStatus? Status);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var tenantId = TenantId ?? throw new UnauthorizedAccessException();
        var users = await userManager.Users.Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.Email).Select(item => new
            {
                item.Id, item.Email, item.Name, item.RoleId, item.LocationScope, item.Status, item.IsOwner,
            }).ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> Invite(InviteUserRequest request)
    {
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
        return CreatedAtAction(nameof(List), new { id = user.Id, token });
    }

    [HttpPost("invitations/{id}/accept")]
    [AllowAnonymous]
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
    public async Task<IActionResult> Update(string id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateUserAccessCommand
        {
            UserId = id,
            RoleId = request.RoleId,
            LocationScope = request.LocationScope,
            Status = request.Status,
        }, cancellationToken);
        return NoContent();
    }

    [HttpGet("/api/v1/roles")]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken) =>
        Ok(await context.AppRoles.AsNoTracking().OrderBy(item => item.Name).Select(item => new
        {
            item.Id, item.Name, permissions = item.Permissions.ToString(), item.MaxDiscountPercent, item.MaxUnauthorizedRefundAmount,
        }).ToListAsync(cancellationToken));

    [HttpGet("/api/v1/audit-log")]
    [Authorize(Roles = "Owner,Administrator")]
    public async Task<IActionResult> AuditLog([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = context.AuditLogEntries.AsNoTracking().OrderByDescending(item => item.OccurredAt);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, totalCount });
    }

    [HttpPut("{userId}/pin")]
    public async Task<IActionResult> SetPin(string userId, SetPinRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetRegisterPinCommand { UserId = userId, Pin = request.Pin }, cancellationToken);
        return NoContent();
    }
}
