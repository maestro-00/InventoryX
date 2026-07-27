using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Commands.Requests.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/auth")]
[Authorize]
public sealed class AuthController(ISender sender) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterTenantResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Created("/api/v1/tenant", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.RequiresTwoFactor
            ? StatusCode(StatusCodes.Status423Locked, new { type = "two_factor_required", detail = "Complete login with a TOTP code." })
            : Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResult>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpPost("google")]
    [AllowAnonymous]
    public IActionResult Google([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl,
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpPost("2fa/enroll")]
    public async Task<ActionResult<TwoFactorEnrollResult>> EnrollTwoFactor(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new EnrollTwoFactorCommand(), cancellationToken));

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Ok(new { enabled = true });
    }
}
