using System.Security.Claims;
using FluentAssertions;
using InventoryX.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;

namespace InventoryX.Presentation.Tests.Middleware;

public sealed class RegisterTokenAuthorizationTests
{
    [Fact]
    public async Task Register_token_may_only_call_matching_register_sync_routes()
    {
        var registerId = Guid.NewGuid();
        var handler = CreateHandler(out var http);
        http.Request.Path = "/api/v1/sync/snapshot";
        http.Request.QueryString = new QueryString($"?registerId={registerId}");

        var allowed = await AuthorizeAsync(handler, RegisterPrincipal(registerId));
        allowed.Succeeded.Should().BeTrue();

        http.Request.QueryString = new QueryString($"?registerId={Guid.NewGuid()}");
        var deniedRegister = await AuthorizeAsync(handler, RegisterPrincipal(registerId));
        deniedRegister.Succeeded.Should().BeFalse();

        http.Request.Path = "/api/v1/products";
        http.Request.QueryString = QueryString.Empty;
        var deniedPath = await AuthorizeAsync(handler, RegisterPrincipal(registerId));
        deniedPath.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task User_token_is_not_constrained_by_register_policy()
    {
        var handler = CreateHandler(out var http);
        http.Request.Path = "/api/v1/products";
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim("tenant_id", Guid.NewGuid().ToString()),
        ], "Bearer");
        var result = await AuthorizeAsync(handler, new ClaimsPrincipal(identity));
        result.Succeeded.Should().BeTrue();
    }

    private static RegisterTokenAuthorizationHandler CreateHandler(out DefaultHttpContext http)
    {
        http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);
        return new RegisterTokenAuthorizationHandler(accessor.Object);
    }

    private static ClaimsPrincipal RegisterPrincipal(Guid registerId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "cashier-1"),
            new Claim("token_scope", "register"),
            new Claim("register_id", registerId.ToString()),
        ], "Bearer");
        return new ClaimsPrincipal(identity);
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        RegisterTokenAuthorizationHandler handler,
        ClaimsPrincipal user)
    {
        var context = new AuthorizationHandlerContext([new RegisterTokenRequirement()], user, null);
        await handler.HandleAsync(context);
        return context.HasSucceeded
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed();
    }
}
