using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace InventoryX.Presentation.Tests.Scenarios;

/// <summary>
/// T031 — quickstart scenario A end-to-end: register → location → product →
/// opening stock → register/shift → sale → stock reads 8 and the sale is in
/// history with correct Ghana tax totals.
/// </summary>
public sealed class FirstSaleScenarioTests : IAsyncLifetime
{
    private readonly TestAppFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(payload).RootElement;
    }

    [Fact]
    public async Task Scenario_A_register_onboard_and_first_sale()
    {
        // 1. Register tenant + owner
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "owner@shop.gh",
            password = "Password1!",
            businessName = "Accra Corner Shop",
            country = "GH",
            currency = "GHS",
            businessType = "Retail",
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created, await registerResponse.Content.ReadAsStringAsync());
        var registration = await ReadJson(registerResponse);
        var accessToken = registration.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrEmpty();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // 2. Create location
        var locationResponse = await _client.PostAsJsonAsync("/api/v1/locations", new { name = "Main Shop", kind = "Shop" });
        locationResponse.StatusCode.Should().Be(HttpStatusCode.Created, await locationResponse.Content.ReadAsStringAsync());
        var locationId = (await ReadJson(locationResponse)).GetProperty("id").GetGuid();

        // 3. Create product (GH-STD tax)
        var productResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Sugar 1kg",
            sku = "SUG-001",
            sellingPrice = 10.00,
            costPrice = 6.00,
            taxTreatmentCode = "GH-STD",
        });
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created, await productResponse.Content.ReadAsStringAsync());
        var productId = (await ReadJson(productResponse)).GetProperty("id").GetGuid();

        // 4. Opening stock: qty 10 @ cost 6.00 via adjustment (reason Correction)
        var stockResponse = await _client.PostAsJsonAsync("/api/v1/stock/adjustments", new
        {
            locationId,
            reasonCode = "Correction",
            note = "Opening stock",
            lines = new[] { new { productId, qtyDelta = 10.0, unitCost = 6.00 } },
        });
        stockResponse.StatusCode.Should().BeOneOf([HttpStatusCode.OK, HttpStatusCode.Created]);

        // 5. Create register and open shift with float 100.00
        var registerCreate = await _client.PostAsJsonAsync("/api/v1/registers", new { locationId, name = "Register 1" });
        registerCreate.StatusCode.Should().Be(HttpStatusCode.Created, await registerCreate.Content.ReadAsStringAsync());
        var registerId = (await ReadJson(registerCreate)).GetProperty("id").GetGuid();

        var shiftResponse = await _client.PostAsJsonAsync($"/api/v1/registers/{registerId}/shifts", new { openingFloat = 100.00 });
        shiftResponse.StatusCode.Should().Be(HttpStatusCode.Created, await shiftResponse.Content.ReadAsStringAsync());
        var shiftId = (await ReadJson(shiftResponse)).GetProperty("id").GetGuid();

        // 6. Sell 2 units cash 25.00 → grandTotal 24.38, change 0.62
        var saleResponse = await _client.PostAsJsonAsync("/api/v1/sales", new
        {
            clientSaleId = Guid.NewGuid(),
            registerId,
            shiftId,
            lines = new[] { new { productId, qty = 2.0 } },
            payments = new[] { new { tender = "Cash", amount = 25.00 } },
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created, await saleResponse.Content.ReadAsStringAsync());
        var sale = await ReadJson(saleResponse);
        sale.GetProperty("grandTotal").GetDecimal().Should().Be(24.38m);
        sale.GetProperty("changeDue").GetDecimal().Should().Be(0.62m);

        // 7. Stock must read 8
        var stockQuery = await _client.GetAsync($"/api/v1/stock?productId={productId}");
        stockQuery.StatusCode.Should().Be(HttpStatusCode.OK, await stockQuery.Content.ReadAsStringAsync());
        var stock = await ReadJson(stockQuery);
        stock.GetProperty("items")[0].GetProperty("qtyOnHand").GetDecimal().Should().Be(8m);

        // 8. Sale appears in history
        var salesList = await _client.GetAsync("/api/v1/sales");
        salesList.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJson(salesList)).GetProperty("items").GetArrayLength().Should().Be(1);
    }
}
