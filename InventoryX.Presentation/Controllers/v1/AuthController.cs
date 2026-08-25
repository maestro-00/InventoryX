using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Application.DTOs.Users;
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
[Tags("Auth")]
public sealed class AuthController(ISender sender, IOptions<JwtOptions> jwtOptions, IConfiguration configuration) : ApiControllerBase
{
    public sealed record RegisterTenantRequest(
        string Email,
        string Password,
        string BusinessName,
        string Country,
        string Currency,
        string BusinessType);

    public sealed record LoginRequest(string Email, string Password, string? TwoFactorCode);
    public sealed record VerifyTwoFactorRequest(string Code);

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterTenantResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterTenantResult>> Register(
        RegisterTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterTenantCommand
        {
            Email = request.Email,
            Password = request.Password,
            BusinessName = request.BusinessName,
            Country = string.IsNullOrWhiteSpace(request.Country) ? "GH" : request.Country,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GHS" : request.Currency,
            BusinessType = string.IsNullOrWhiteSpace(request.BusinessType) ? "Retail" : request.BusinessType,
        }, cancellationToken);
        AuthSessionCookies.Set(Response, result.RefreshToken, jwtOptions);
        return Created("/api/v1/tenant", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            TwoFactorCode = request.TwoFactorCode,
        }, cancellationToken);
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
        var allowedOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
        var properties = new AuthenticationProperties();
        properties.Items["returnUrl"] = SafeReturnUrl.Normalize(returnUrl, allowedOrigins);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpPost("2fa/enroll")]
    public async Task<ActionResult<TwoFactorEnrollResult>> EnrollTwoFactor(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new EnrollTwoFactorCommand(), cancellationToken));

    [HttpPost("2fa/verify")]
    [ProducesResponseType(typeof(TwoFactorVerifyResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorVerifyResultDto>> VerifyTwoFactor(
        VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new VerifyTwoFactorCommand { Code = request.Code }, cancellationToken);
        return Ok(new TwoFactorVerifyResultDto(true));
    }
}
