using Microsoft.Extensions.Options;
using InventoryX.Application.Options;

namespace InventoryX.Presentation.Authentication;

/// <summary>
/// SPA session durability: httpOnly refresh cookie for silent restore, plus a readable
/// <c>inventoryx_session</c> marker so anonymous cold loads never hit /auth/refresh.
/// </summary>
public static class AuthSessionCookies
{
    public const string RefreshTokenCookieName = "inventoryx_refresh";
    public const string SessionMarkerCookieName = "inventoryx_session";
    public const string RefreshCookiePath = "/api/v1/auth";

    public static void Set(HttpResponse response, string refreshToken, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = RefreshCookiePath,
            IsEssential = true,
            Expires = expires,
        });
        response.Cookies.Append(SessionMarkerCookieName, "1", new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            Expires = expires,
        });
    }

    public static void Set(HttpResponse response, string refreshToken, IOptions<JwtOptions> jwtOptions) =>
        Set(response, refreshToken, TimeSpan.FromDays(Math.Max(1, jwtOptions.Value.RefreshTokenDays)));

    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = RefreshCookiePath,
        });
        response.Cookies.Delete(SessionMarkerCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
    }

    public static string? ReadRefreshToken(HttpRequest request) =>
        request.Cookies.TryGetValue(RefreshTokenCookieName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}
