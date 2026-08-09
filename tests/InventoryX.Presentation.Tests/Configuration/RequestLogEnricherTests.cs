using System.Security.Claims;
using FluentAssertions;
using InventoryX.Presentation.Configuration;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Presentation.Tests.Configuration;

public sealed class RequestLogEnricherTests
{
    [Fact]
    public void Authenticated_request_includes_tenant_user_and_trace()
    {
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
            ], "TestAuth")),
        };

        var identity = RequestLogEnricher.Read(context);

        identity.TenantId.Should().Be(tenantId.ToString());
        identity.UserId.Should().Be("user-1");
        identity.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Anonymous_request_still_includes_stable_identity_properties()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-anonymous",
        };

        var identity = RequestLogEnricher.Read(context);

        identity.TenantId.Should().Be("anonymous");
        identity.UserId.Should().Be("anonymous");
        identity.TraceId.Should().NotBeNullOrWhiteSpace();
    }
}
