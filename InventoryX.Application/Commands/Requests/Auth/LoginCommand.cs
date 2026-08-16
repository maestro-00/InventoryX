using MediatR;

namespace InventoryX.Application.Commands.Requests.Auth
{
    public record LoginResult(
        bool RequiresTwoFactor,
        string? AccessToken,
        DateTime? AccessTokenExpiresAt,
        string? RefreshToken);

    public class LoginCommand : IRequest<LoginResult>
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
        /// <summary>TOTP code when completing a 2FA-required login.</summary>
        public string? TwoFactorCode { get; init; }
    }

    public class RefreshTokenCommand : IRequest<LoginResult>
    {
        /// <summary>
        /// Optional when the httpOnly <c>inventoryx_refresh</c> cookie is present (silent SPA restore).
        /// </summary>
        public string? RefreshToken { get; init; }
    }

    public record TwoFactorEnrollResult(string SharedKey, string AuthenticatorUri);

    public class EnrollTwoFactorCommand : IRequest<TwoFactorEnrollResult>;

    public class VerifyTwoFactorCommand : IRequest<bool>
    {
        public required string Code { get; init; }
    }
}
