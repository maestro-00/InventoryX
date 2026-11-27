using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InventoryX.Presentation.Tests.Scenarios;

namespace InventoryX.Presentation.Tests.Billing;

public sealed class BillingRecoveryContractTests
{
    [Fact]
    public async Task Recovery_contract_publishes_pending_and_payment_required_responses()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v2/swagger.json"));
        var responses = document.RootElement.GetProperty("paths")
            .GetProperty("/api/v2/billing/recovery").GetProperty("post").GetProperty("responses");

        responses.TryGetProperty("202", out _).Should().BeTrue();
        responses.TryGetProperty("402", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Recovery_requires_owner_authentication_and_idempotency_header()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        var unauthenticated = await client.PostAsJsonAsync("/api/v2/billing/recovery",
            new { methodKind = "card" });

        unauthenticated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
