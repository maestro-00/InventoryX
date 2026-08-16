using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InventoryX.Presentation.Authentication;
using Xunit;

namespace InventoryX.Presentation.Tests.Scenarios;

/// <summary>
/// SPA session durability: httpOnly refresh cookie + readable inventoryx_session marker.
/// </summary>
public sealed class AuthSessionCookieTests : IAsyncLifetime
{
    private readonly TestAppFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static IReadOnlyList<string> SetCookieHeaders(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToList()
            : [];

    [Fact]
    public async Task Register_sets_refresh_and_session_marker_cookies()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "cookie-owner@shop.gh",
            password = "Password1!",
            businessName = "Cookie Shop",
            country = "GH",
            currency = "GHS",
            businessType = "Retail",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var cookies = SetCookieHeaders(response);
        cookies.Should().Contain(c =>
            c.StartsWith($"{AuthSessionCookies.RefreshTokenCookieName}=", StringComparison.Ordinal) &&
            c.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            c.Contains($"path={AuthSessionCookies.RefreshCookiePath}", StringComparison.OrdinalIgnoreCase));
        cookies.Should().Contain(c =>
            c.StartsWith($"{AuthSessionCookies.SessionMarkerCookieName}=1", StringComparison.Ordinal) &&
            !c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_without_body_uses_cookie_and_rotates_session_cookies()
    {
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "refresh-cookie@shop.gh",
            password = "Password1!",
            businessName = "Refresh Shop",
            country = "GH",
            currency = "GHS",
            businessType = "Retail",
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var registration = JsonDocument.Parse(await register.Content.ReadAsStringAsync()).RootElement;
        var refreshToken = registration.GetProperty("refreshToken").GetString();
        refreshToken.Should().NotBeNullOrEmpty();

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{AuthSessionCookies.RefreshTokenCookieName}={refreshToken}");
        var refresh = await _client.SendAsync(refreshRequest);

        refresh.StatusCode.Should().Be(HttpStatusCode.OK, await refresh.Content.ReadAsStringAsync());
        var body = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);

        var cookies = SetCookieHeaders(refresh);
        cookies.Should().Contain(c => c.StartsWith($"{AuthSessionCookies.RefreshTokenCookieName}=", StringComparison.Ordinal));
        cookies.Should().Contain(c => c.StartsWith($"{AuthSessionCookies.SessionMarkerCookieName}=1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Refresh_with_invalid_cookie_clears_session_cookies()
    {
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{AuthSessionCookies.RefreshTokenCookieName}=not-a-real-token");
        var refresh = await _client.SendAsync(refreshRequest);

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var cookies = SetCookieHeaders(refresh);
        cookies.Should().Contain(c =>
            c.StartsWith($"{AuthSessionCookies.RefreshTokenCookieName}=", StringComparison.Ordinal) &&
            (c.Contains("expires=", StringComparison.OrdinalIgnoreCase) || c.Contains("max-age=0", StringComparison.OrdinalIgnoreCase)));
        cookies.Should().Contain(c =>
            c.StartsWith($"{AuthSessionCookies.SessionMarkerCookieName}=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Logout_clears_session_cookies()
    {
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookies = SetCookieHeaders(response);
        cookies.Should().Contain(c => c.StartsWith($"{AuthSessionCookies.RefreshTokenCookieName}=", StringComparison.Ordinal));
        cookies.Should().Contain(c => c.StartsWith($"{AuthSessionCookies.SessionMarkerCookieName}=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Refresh_without_cookie_or_body_stays_unauthorized_and_clears_markers()
    {
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        SetCookieHeaders(response).Should().NotBeEmpty();
    }
}
