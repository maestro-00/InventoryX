using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Options;
using InventoryX.Presentation.Authentication;
using InventoryX.Presentation.Swagger;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/auth")]
[Authorize]
public sealed class AuthController(ISender sender, IOptions<JwtOptions> jwtOptions) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterTenantResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        AuthSessionCookies.Set(Response, result.RefreshToken, jwtOptions);
        return Created("/api/v1/tenant", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.RequiresTwoFactor)
        {
            return StatusCode(StatusCodes.Status423Locked, new { type = "two_factor_required", detail = "Complete login with a TOTP code." });
        }

        if (!string.IsNullOrWhiteSpace(result.RefreshToken))
            AuthSessionCookies.Set(Response, result.RefreshToken, jwtOptions);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenCommand? command,
        CancellationToken cancellationToken)
    {
        var refreshToken = !string.IsNullOrWhiteSpace(command?.RefreshToken)
            ? command.RefreshToken
            : AuthSessionCookies.ReadRefreshToken(Request);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            AuthSessionCookies.Clear(Response);
            return Unauthorized();
        }

        try
        {
            var result = await sender.Send(new RefreshTokenCommand { RefreshToken = refreshToken }, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                AuthSessionCookies.Set(Response, result.RefreshToken, jwtOptions);
            return Ok(result);
        }
        catch (CustomException ex) when (ex.StatusCode is StatusCodes.Status400BadRequest
            or StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden)
        {
            AuthSessionCookies.Clear(Response);
            return StatusCode(ex.StatusCode, new ProblemDetails
            {
                Type = "https://inventoryx.app/problems/unauthorized",
                Title = "Refresh token rejected.",
                Status = ex.StatusCode,
                Detail = ex.Message,
            });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        AuthSessionCookies.Clear(Response);
        return Ok();
    }

    [HttpPost("pin/exchange")]
    [LiveOnly("PIN exchange requires an authenticated device session and live credential verification.")]
    public async Task<ActionResult<RegisterPinExchangeResult>> ExchangePin(
        ExchangeRegisterPinCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpGet("google")]
    [HttpPost("google")]
    [AllowAnonymous]
    public IActionResult Google([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties();
        properties.Items["returnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
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
