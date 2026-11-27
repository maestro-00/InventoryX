using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryX.Presentation.Tests.Scenarios;

namespace InventoryX.Presentation.Tests.Billing;

public sealed class BillingChangeContractTests
{
    [Fact]
    public async Task V2_openapi_publishes_change_cancellation_and_recovery_contract()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v2/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");

        paths.GetProperty("/api/v2/billing/changes").GetProperty("post")
            .GetProperty("responses").TryGetProperty("202", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/changes/{id}").GetProperty("delete")
            .GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/cancellation").GetProperty("post")
            .GetProperty("responses").TryGetProperty("202", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/cancellation").GetProperty("delete")
            .GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/recovery").GetProperty("post")
            .GetProperty("responses").TryGetProperty("202", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Private_change_and_recovery_routes_require_authentication()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        var change = await client.PostAsJsonAsync("/api/v2/billing/changes", new
        {
            quoteId = Guid.NewGuid(), acknowledgeExceededLimits = false,
        });
        change.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var recovery = await client.PostAsJsonAsync("/api/v2/billing/recovery", new { methodKind = "card" });
        recovery.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
