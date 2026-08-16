using FluentAssertions;
using InventoryX.Presentation.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InventoryX.Presentation.Tests.Authentication;

public sealed class AuthSessionCookiesTests
{
    [Fact]
    public void Set_writes_httpOnly_refresh_and_readable_session_marker()
    {
        var context = new DefaultHttpContext();

        AuthSessionCookies.Set(context.Response, "refresh-secret", TimeSpan.FromDays(14));

        var setCookie = context.Response.Headers.SetCookie.ToString();
        setCookie.Should().Contain($"{AuthSessionCookies.RefreshTokenCookieName}=refresh-secret");
        setCookie.Should().Contain("httponly", Exactly.Once());
        setCookie.Should().Contain($"path={AuthSessionCookies.RefreshCookiePath}");
        setCookie.Should().Contain($"{AuthSessionCookies.SessionMarkerCookieName}=1");
        setCookie.Should().Contain("path=/");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("samesite=lax");
    }

    [Fact]
    public void Clear_expires_both_cookies_with_matching_paths()
    {
        var context = new DefaultHttpContext();
        AuthSessionCookies.Set(context.Response, "refresh-secret", TimeSpan.FromDays(1));

        AuthSessionCookies.Clear(context.Response);

        var setCookie = context.Response.Headers.SetCookie.ToString();
        setCookie.Should().Contain($"{AuthSessionCookies.RefreshTokenCookieName}=;");
        setCookie.Should().Contain($"{AuthSessionCookies.SessionMarkerCookieName}=;");
        setCookie.Should().Contain($"path={AuthSessionCookies.RefreshCookiePath}");
        setCookie.Should().Contain("path=/");
    }

    [Fact]
    public void ReadRefreshToken_returns_cookie_value_when_present()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{AuthSessionCookies.RefreshTokenCookieName}=from-cookie";

        AuthSessionCookies.ReadRefreshToken(context.Request).Should().Be("from-cookie");
    }

    [Fact]
    public void ReadRefreshToken_returns_null_when_missing()
    {
        var context = new DefaultHttpContext();
        AuthSessionCookies.ReadRefreshToken(context.Request).Should().BeNull();
    }
}
