using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services.IServices
{
    public record TokenPair(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

    /// <summary>
    /// Issues JWTs carrying tenant_id, role and location_scope claims
    /// (research R3). Register-scoped tokens are limited to POS operations on
    /// one register.
    /// </summary>
    public interface ITokenService
    {
        TokenPair CreateTokenPair(User user, Role? role);

        /// <summary>Short-lived register-scoped token from a PIN exchange (FR-007).</summary>
        string CreateRegisterScopedToken(User user, Role? role, Guid registerId);

        /// <summary>Validates a refresh token and returns the user id it was issued to, or null.</summary>
        string? ValidateRefreshToken(string refreshToken);
    }
}
