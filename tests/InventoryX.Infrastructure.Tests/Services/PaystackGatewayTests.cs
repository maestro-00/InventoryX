using System.Net;
using System.Text;
using FluentAssertions;
using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using InventoryX.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace InventoryX.Infrastructure.Tests.Services;

public sealed class PaystackGatewayTests
{
    [Fact]
    public async Task Initializes_card_and_maps_ghana_mobile_money_provider_codes()
    {
        var requests = new List<(string Path, string Body, string? Authorization)>();
        var handler = new StubHandler(async request =>
        {
            requests.Add((request.RequestUri!.AbsolutePath,
                request.Content is null ? "" : await request.Content.ReadAsStringAsync(),
                request.Headers.Authorization?.ToString()));
            var json = request.RequestUri.AbsolutePath.EndsWith("initialize")
                ? """{"status":true,"data":{"reference":"ref-1","authorization_url":"https://checkout","access_code":"access"}}"""
                : """{"status":true,"data":{"reference":"ref-2","status":"pay_offline","display_text":"Approve on phone"}}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co/") };
        var gateway = new PaystackGateway(client, Options.Create(new PaystackOptions { SecretKey = "sk_test" }));

        var initialized = await gateway.InitializeAuthorizationAsync(
            new PaymentInitializationRequest("owner@example.com", 12.34m, Channel: "card"));
        var charged = await gateway.ChargeAsync(new PaymentChargeRequest(
            "owner@example.com", 10m, MobileMoneyProvider: "at", Msisdn: "0551234567"));

        initialized.Reference.Should().Be("ref-1");
        charged.Status.Should().Be("pay_offline");
        requests[0].Body.Should().Contain("1234").And.Contain("card");
        requests[1].Body.Should().Contain("\"provider\":\"tgo\"");
        requests.Should().OnlyContain(request => request.Authorization == "Bearer sk_test");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }
}
