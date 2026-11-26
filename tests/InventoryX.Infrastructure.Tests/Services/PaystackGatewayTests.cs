using System.Net;
using System.Text;
using System.Text.Json;
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
        requests[1].Body.Should().Contain("\"provider\":\"atl\"");
        requests.Should().OnlyContain(request => request.Authorization == "Bearer sk_test");
    }

    [Theory]
    [InlineData("mtn", "mtn")]
    [InlineData("telecel", "vod")]
    [InlineData("airteltigo", "atl")]
    public async Task Mobile_money_uses_supported_ghana_provider_codes_and_pending_state(string provider, string expectedCode)
    {
        string? posted = null;
        var handler = new StubHandler(async request =>
        {
            posted = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":true,"data":{"reference":"momo-1","status":"pay_offline"}}"""),
            };
        });
        var gateway = new PaystackGateway(new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co/") },
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }));

        var result = await gateway.ChargeAsync(new PaymentChargeRequest(
            "owner@example.com", 12.34m, MobileMoneyProvider: provider, Msisdn: "0551234567"));

        result.Status.Should().Be("pay_offline");
        posted.Should().Contain("\"amount\":1234").And.Contain($"\"provider\":\"{expectedCode}\"");
    }

    [Theory]
    [InlineData("mobile_money", true, false)]
    [InlineData("card", false, false)]
    [InlineData("card", true, true)]
    public async Task Only_explicitly_reusable_card_authorizations_are_returned(
        string channel, bool reusable, bool shouldReturnCode)
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                status = true,
                data = new
                {
                    reference = "verify-1", status = "success", amount = 1234, currency = "GHS",
                    authorization = new { channel, reusable, authorization_code = "AUTH-1" },
                },
            })),
        }));
        var gateway = new PaystackGateway(new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co/") },
            Options.Create(new PaystackOptions { SecretKey = "sk_test" }));

        var result = await gateway.VerifyAsync("verify-1");

        result.Amount.Should().Be(12.34m);
        result.AuthorizationCode.Should().Be(shouldReturnCode ? "AUTH-1" : null);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }
}
