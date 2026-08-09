using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using Microsoft.Extensions.Options;

namespace InventoryX.Infrastructure.Services;

public sealed class PaystackGateway(HttpClient httpClient, IOptions<PaystackOptions> options) : IPaymentGateway
{
    private readonly PaystackOptions _options = options.Value;

    public async Task<PaymentInitializationResult> InitializeAuthorizationAsync(
        PaymentInitializationRequest request, CancellationToken cancellationToken = default)
    {
        var channel = NormalizeChannel(request.Channel);
        var response = await SendAsync(HttpMethod.Post, "transaction/initialize", new
        {
            request.Email,
            amount = ToSubunit(request.Amount),
            request.Currency,
            channels = new[] { channel },
            request.Reference,
            callback_url = request.CallbackUrl ?? _options.CallbackUrl,
        }, cancellationToken);
        var data = response.GetProperty("data");
        return new PaymentInitializationResult(
            data.GetProperty("reference").GetString()!,
            data.GetProperty("authorization_url").GetString()!,
            data.GetProperty("access_code").GetString()!);
    }

    public async Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        JsonElement response;
        if (!string.IsNullOrWhiteSpace(request.AuthorizationCode))
        {
            response = await SendAsync(HttpMethod.Post, "transaction/charge_authorization", new
            {
                request.Email, amount = ToSubunit(request.Amount), request.Currency,
                authorization_code = request.AuthorizationCode, request.Reference, queue = true,
            }, cancellationToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.MobileMoneyProvider) || string.IsNullOrWhiteSpace(request.Msisdn))
                throw new FluentValidation.ValidationException("Mobile money charges require provider and msisdn.");
            response = await SendAsync(HttpMethod.Post, "charge", new
            {
                request.Email, amount = ToSubunit(request.Amount), request.Currency, request.Reference,
                mobile_money = new { phone = request.Msisdn, provider = NormalizeProvider(request.MobileMoneyProvider) },
            }, cancellationToken);
        }
        var data = response.GetProperty("data");
        return new PaymentChargeResult(
            data.GetProperty("reference").GetString()!,
            data.GetProperty("status").GetString()!,
            data.TryGetProperty("display_text", out var text) ? text.GetString() : null);
    }

    public async Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(HttpMethod.Get, $"transaction/verify/{Uri.EscapeDataString(reference)}", null, cancellationToken);
        var data = response.GetProperty("data");
        string? authorizationCode = null;
        if (data.TryGetProperty("authorization", out var authorization) &&
            authorization.TryGetProperty("authorization_code", out var code)) authorizationCode = code.GetString();
        DateTime? paidAt = data.TryGetProperty("paid_at", out var paid) && paid.ValueKind == JsonValueKind.String
            ? paid.GetDateTime() : null;
        return new PaymentVerificationResult(
            data.GetProperty("reference").GetString()!, data.GetProperty("status").GetString()!,
            data.GetProperty("amount").GetInt64() / 100m, data.GetProperty("currency").GetString()!,
            authorizationCode, paidAt);
    }

    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        if (body is not null) message.Content = JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)
        { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        using var response = await httpClient.SendAsync(message, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new CustomException($"Paystack request failed ({(int)response.StatusCode}): {payload}", 502);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement.Clone();
        if (!root.TryGetProperty("status", out var status) || !status.GetBoolean())
            throw new CustomException(root.TryGetProperty("message", out var detail) ? detail.GetString() ?? "Paystack request failed." : "Paystack request failed.", 502);
        return root;
    }

    private static long ToSubunit(decimal amount)
    {
        if (amount <= 0) throw new FluentValidation.ValidationException("Payment amount must be greater than zero.");
        return checked((long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }

    private static string NormalizeChannel(string channel) => channel.Trim().ToLowerInvariant() switch
    {
        "card" => "card",
        "mobile_money" or "mobilemoney" => "mobile_money",
        _ => throw new FluentValidation.ValidationException("Channel must be card or mobile_money."),
    };

    private static string NormalizeProvider(string provider) => provider.Trim().ToLowerInvariant() switch
    {
        "mtn" => "mtn",
        "telecel" or "vodafone" or "vod" => "vod",
        "at" or "airteltigo" or "tgo" => "tgo",
        _ => throw new FluentValidation.ValidationException("Mobile money provider must be mtn, telecel, or at."),
    };
}
