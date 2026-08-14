using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Presentation.Authorization;

/// <summary>
/// Register-scoped JWTs (token_scope=register) may only call sync routes and
/// only for the register_id claim they carry.
/// </summary>
public sealed class RegisterTokenRequirement : IAuthorizationRequirement;

public sealed class RegisterTokenAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<RegisterTokenRequirement>
{
    public static readonly HashSet<string> AllowedSyncPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/sync/sales",
        "/api/v1/sync/snapshot",
    };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RegisterTokenRequirement requirement)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var scope = context.User.FindFirstValue("token_scope");
        if (!string.Equals(scope, "register", StringComparison.OrdinalIgnoreCase))
        {
            // Normal user tokens are not constrained by this policy.
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var path = http.Request.Path.Value ?? string.Empty;
        var allowedPath = AllowedSyncPaths.Any(allowed =>
            path.Equals(allowed, StringComparison.OrdinalIgnoreCase));
        if (!allowedPath)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var claimRegister = context.User.FindFirstValue("register_id");
        if (!Guid.TryParse(claimRegister, out var claimedRegisterId))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Snapshot declares registerId in the query string — enforce match here.
        if (path.Equals("/api/v1/sync/snapshot", StringComparison.OrdinalIgnoreCase))
        {
            if (!http.Request.Query.TryGetValue("registerId", out var queryValue) ||
                !Guid.TryParse(queryValue.FirstOrDefault(), out var requestRegisterId) ||
                requestRegisterId != claimedRegisterId)
            {
                context.Fail();
                return Task.CompletedTask;
            }
        }

        // POST /sync/sales body is not available yet; claim is stamped for handler enforcement.
        http.Items["RegisterToken.ClaimedRegisterId"] = claimedRegisterId;
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
