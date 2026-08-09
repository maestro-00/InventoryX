using System.Security.Claims;
using FluentAssertions;
using InventoryX.Application.Services.IServices;
using InventoryX.Presentation.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryX.Presentation.Tests.Middleware;

/// <summary>
/// T009 — the tenant-resolution middleware must populate ITenantContext from the
/// authenticated principal's tenant_id claim and reject authenticated requests
/// that carry no tenant claim with 401.
/// </summary>
public class TenantResolutionMiddlewareTests
{
    private sealed class MutableTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public string? LocationScope { get; set; }
    }

    private static DefaultHttpContext AuthenticatedContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
        return context;
    }

    [Fact]
    public async Task Populates_tenant_context_from_claims()
    {
        var tenantId = Guid.NewGuid();
        var httpContext = AuthenticatedContext(
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, "Owner"),
            new Claim("location_scope", "*"));

        var tenantContext = new MutableTenantContext();
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<TenantResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, tenantContext);

        nextCalled.Should().BeTrue();
        tenantContext.TenantId.Should().Be(tenantId);
        tenantContext.UserId.Should().Be("user-1");
        tenantContext.Role.Should().Be("Owner");
        tenantContext.LocationScope.Should().Be("*");
    }

    [Fact]
    public async Task Authenticated_user_without_tenant_claim_gets_401()
    {
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "user-1"));
        var tenantContext = new MutableTenantContext();
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<TenantResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, tenantContext);

        nextCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Anonymous_request_passes_through_without_tenant()
    {
        var httpContext = new DefaultHttpContext();
        var tenantContext = new MutableTenantContext();
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<TenantResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, tenantContext);

        nextCalled.Should().BeTrue();
        tenantContext.TenantId.Should().BeNull();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Malformed_tenant_claim_gets_401()
    {
        var httpContext = AuthenticatedContext(new Claim("tenant_id", "not-a-guid"));
        var tenantContext = new MutableTenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask,
            NullLogger<TenantResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, tenantContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        tenantContext.TenantId.Should().BeNull();
    }
}
