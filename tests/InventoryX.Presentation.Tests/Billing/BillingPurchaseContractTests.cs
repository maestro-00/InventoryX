using System.Net;
using System.Text.Json;
using FluentAssertions;
using InventoryX.Presentation.Tests.Scenarios;

namespace InventoryX.Presentation.Tests.Billing;

public sealed class BillingPurchaseContractTests
{
    [Fact]
    public async Task V2_openapi_publishes_purchase_routes_and_response_states()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v2/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");

        var plans = paths.GetProperty("/api/v2/billing/plans").GetProperty("get");
        plans.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        var quote = paths.GetProperty("/api/v2/billing/quotes").GetProperty("post");
        quote.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        quote.GetProperty("responses").TryGetProperty("409", out _).Should().BeTrue();
        var purchase = paths.GetProperty("/api/v2/billing/payment-intents").GetProperty("post");
        purchase.GetProperty("responses").TryGetProperty("201", out _).Should().BeTrue();
        purchase.GetProperty("responses").TryGetProperty("202", out _).Should().BeTrue();
        purchase.GetProperty("responses").TryGetProperty("409", out _).Should().BeTrue();
        purchase.GetProperty("responses").TryGetProperty("402", out _).Should().BeTrue();
        var status = paths.GetProperty("/api/v2/billing/payment-intents/{id}").GetProperty("get");
        status.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        status.GetProperty("parameters").EnumerateArray()
            .Should().Contain(parameter => parameter.GetProperty("name").GetString() == "id");
    }

    [Fact]
    public async Task Public_plans_are_available_but_private_purchase_state_requires_authentication()
    {
        await using var factory = new TestAppFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var plans = await client.GetAsync("/api/v2/billing/plans");
        plans.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await plans.Content.ReadAsStringAsync();
        body.Should().Contain("Standard").And.Contain("Professional");
        body.Should().NotContain("authorizationCode").And.NotContain("billingEmail");

        (await client.GetAsync($"/api/v2/billing/payment-intents/{Guid.NewGuid()}"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
