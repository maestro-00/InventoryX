using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InventoryX.Infrastructure.Services
{
    /// <summary>
    /// Issues JWTs carrying tenant_id, role and location_scope claims (T018,
    /// research R3). Refresh tokens are opaque random values held server-side
    /// with an expiry.
    /// </summary>
    public class JwtTokenService(IOptions<JwtOptions> jwtOptions, IMemoryCache refreshTokenStore) : ITokenService
    {
        private readonly JwtOptions _options = jwtOptions.Value;

        public TokenPair CreateTokenPair(User user, Role? role)
        {
            var accessExpiry = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
            var accessToken = CreateToken(BuildClaims(user, role), accessExpiry);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshExpiry = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
            refreshTokenStore.Set($"refresh:{refreshToken}", user.Id, refreshExpiry);

            return new TokenPair(accessToken, accessExpiry, refreshToken, refreshExpiry);
        }

        public string CreateRegisterScopedToken(User user, Role? role, Guid registerId)
        {
            var claims = BuildClaims(user, role);
            claims.Add(new Claim("register_id", registerId.ToString()));
            claims.Add(new Claim("token_scope", "register"));
            return CreateToken(claims, DateTime.UtcNow.AddMinutes(_options.RegisterTokenMinutes));
        }

        public string? ValidateRefreshToken(string refreshToken) =>
            refreshTokenStore.TryGetValue($"refresh:{refreshToken}", out string? userId) ? userId : null;

        public void RevokeRefreshToken(string refreshToken) =>
            refreshTokenStore.Remove($"refresh:{refreshToken}");

        private static List<Claim> BuildClaims(User user, Role? role)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            if (user.TenantId is not null) claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));
            if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role.Name));
            claims.Add(new Claim("location_scope", user.LocationScope ?? "*"));
            if (user.IsOwner) claims.Add(new Claim("is_owner", "true"));
            return claims;
        }

        private string CreateToken(IEnumerable<Claim> claims, DateTime expires)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ResolveSigningKey()));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
