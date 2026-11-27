using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryX.Application.Options;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryX.Infrastructure.Services;

public sealed record PaystackWebhookResult(bool AlreadyProcessed, string EventType, string? Reference);

/// <summary>Verifies Paystack callbacks and applies charge events exactly once.</summary>
public sealed class PaystackWebhookProcessor(
    AppDbContext context, IOptions<PaystackOptions> options, IBillingInvoiceService invoiceService,
    IPaymentGateway? gateway = null, ITenantContext? tenantContext = null,
    IBillingSettlementService? settlement = null)
{
    private readonly string _secretKey = options.Value.SecretKey;
    private readonly PaystackOptions _options = options.Value;

    public PaystackSignatureMode? VerifySignature(byte[] payload, string? signingHeader, string? legacyHeader)
    {
        if (!string.IsNullOrWhiteSpace(signingHeader))
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookSigningSecret)) return null;
            long? timestamp = null;
            var signatures = new List<string>();
            foreach (var part in signingHeader.Split(','))
            {
                var pair = part.Trim().Split('=', 2);
                if (pair.Length != 2) continue;
                if (pair[0] == "t")
                {
                    if (timestamp is not null || !long.TryParse(pair[1], out var seconds)) return null;
                    timestamp = seconds;
                }
                else if (pair[0] == "v1")
                {
                    var signature = pair[1].Split(':', 2);
                    if (signature.Length == 2 && signature[0].StartsWith("whk_", StringComparison.Ordinal))
                        signatures.Add(signature[1]);
                }
            }
            if (timestamp is null || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp.Value) > 300)
                return null;
            var prefix = Encoding.ASCII.GetBytes($"{timestamp}.");
            var signedBytes = new byte[prefix.Length + payload.Length];
            prefix.CopyTo(signedBytes, 0);
            payload.CopyTo(signedBytes, prefix.Length);
            var expected = HMACSHA512.HashData(Encoding.UTF8.GetBytes(_options.WebhookSigningSecret), signedBytes);
            foreach (var signature in signatures)
            {
                if (signature.Length != 128) continue;
                try
                {
                    if (CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signature)))
                        return PaystackSignatureMode.SigningSecret;
                }
                catch (FormatException) { }
            }
            return null;
        }

        return _options.AcceptLegacyWebhookSignature &&
               VerifySignature(Encoding.UTF8.GetString(payload), legacyHeader)
            ? PaystackSignatureMode.LegacySecret : null;
    }

    public bool VerifySignature(string payload, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_secretKey) || string.IsNullOrWhiteSpace(signature)) return false;
        var expected = HMACSHA512.HashData(Encoding.UTF8.GetBytes(_secretKey), Encoding.UTF8.GetBytes(payload));
        try
        {
            var supplied = Convert.FromHexString(signature);
            return CryptographicOperations.FixedTimeEquals(expected, supplied);
        }
        catch (FormatException) { return false; }
    }

    /// <summary>Writes a signed delivery durably without calling Paystack.</summary>
    public async Task<PaystackWebhookResult> EnqueueAsync(string payload,
        PaystackSignatureMode signatureMode, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = GetRequiredString(root, "event");
        BillingMetrics.WebhooksReceived.Add(1,
            new KeyValuePair<string, object?>("event", eventType));
        var data = root.TryGetProperty("data", out var parsedData) ? parsedData : default;
        var reference = GetOptionalString(data, "reference");
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var eventId = GetOptionalIdentifier(root, "id") ?? GetOptionalIdentifier(data, "id")
            ?? $"{eventType}:{payloadHash}";

        var existingInbox = await context.PaystackEventInbox.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.ProviderEventId == eventId, cancellationToken);
        if (existingInbox is not null)
        {
            BillingMetrics.WebhookDuplicates.Add(1);
            return new PaystackWebhookResult(true, eventType, reference);
        }

        var attempt = string.IsNullOrWhiteSpace(reference) ? null :
            await context.PaymentAttempts.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.Reference == reference, cancellationToken);
        var inbox = new PaystackEventInbox
        {
            TenantId = attempt?.TenantId,
            ProviderEventId = eventId,
            PayloadHash = payloadHash,
            Payload = payload,
            Reference = reference,
            EventType = eventType,
            ReceivedAt = DateTime.UtcNow,
            SignatureMode = signatureMode,
        };
        context.PaystackEventInbox.Add(inbox);
        await context.SaveChangesAsync(cancellationToken);
        return new PaystackWebhookResult(false, eventType, reference);
    }

    /// <summary>Processes durable deliveries; unresolved references stay in the inbox.</summary>
    public async Task ReconcilePendingAsync(CancellationToken cancellationToken = default)
    {
        if (gateway is null || tenantContext is null || settlement is null) return;
        var pending = await context.PaystackEventInbox.IgnoreQueryFilters()
            .Where(item => item.Status == PaystackEventStatus.Received && item.Reference != null &&
                context.PaymentAttempts.IgnoreQueryFilters().Any(attempt => attempt.Reference == item.Reference))
            .OrderBy(item => item.ReceivedAt).Take(50).ToListAsync(cancellationToken);
        foreach (var inbox in pending)
        {
            if (string.IsNullOrWhiteSpace(inbox.Reference)) continue;
            var attempt = await context.PaymentAttempts.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.Reference == inbox.Reference, cancellationToken);
            if (attempt is null) continue;
            inbox.TenantId = attempt.TenantId;
            tenantContext.TenantId = attempt.TenantId;
            if (inbox.EventType is not ("charge.success" or "charge.failed"))
            {
                inbox.Status = PaystackEventStatus.Processed;
                inbox.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                continue;
            }

            PaymentVerificationResult verification;
            try { verification = await gateway.VerifyAsync(attempt.Reference, cancellationToken); }
            catch
            {
                inbox.SafeErrorSummary = "Provider verification unavailable.";
                await context.SaveChangesAsync(cancellationToken);
                continue;
            }
            if (verification.Reference == attempt.Reference &&
                verification.Amount == attempt.Amount &&
                string.Equals(verification.Currency, attempt.Currency, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(verification.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    await settlement.SettleAsync(attempt.Reference, verification.Amount, verification.Currency,
                        verification.PaidAt ?? DateTime.UtcNow, cancellationToken, verification.AuthorizationCode,
                        verification.CardBrand, verification.CardLast4,
                        verification.CardExpiryMonth, verification.CardExpiryYear);
                    inbox.Status = PaystackEventStatus.Processed;
                    inbox.ProcessedAt = DateTime.UtcNow;
                }
                else if (string.Equals(verification.Status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    var trackedAttempt = await context.PaymentAttempts.SingleAsync(a => a.Id == attempt.Id, cancellationToken);
                    var intent = await context.PaymentIntents.SingleAsync(i => i.Id == trackedAttempt.PaymentIntentId, cancellationToken);
                    if (intent.Status != PaymentIntentStatus.Succeeded)
                    {
                        trackedAttempt.MarkFailed(DateTime.UtcNow, "provider_failed");
                        intent.MarkFailed();
                        if (trackedAttempt.PaymentMethodId is { } methodId)
                        {
                            var pendingMethod = await context.PaymentMethods.SingleAsync(
                                method => method.Id == methodId, cancellationToken);
                            pendingMethod.Invalidate();
                        }
                    }
                    inbox.Status = PaystackEventStatus.Processed;
                    inbox.ProcessedAt = DateTime.UtcNow;
                }
                await context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                BillingMetrics.WebhookMismatches.Add(1);
                inbox.Status = PaystackEventStatus.Failed;
                inbox.SafeErrorSummary = "Provider verification mismatch.";
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>Compatibility entry point for focused settlement tests.</summary>
    public async Task<PaystackWebhookResult> ProcessAsync(string payload, CancellationToken cancellationToken = default,
        PaystackSignatureMode signatureMode = PaystackSignatureMode.LegacySecret)
    {
        var result = await EnqueueAsync(payload, signatureMode, cancellationToken);
        if (!result.AlreadyProcessed) await ReconcilePendingAsync(cancellationToken);
        return result;
    }

    private static string GetRequiredString(JsonElement element, string property) =>
        GetOptionalString(element, property) ?? throw new InvalidOperationException($"Paystack webhook has no {property}.");

    private static string? GetOptionalString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static string? GetOptionalIdentifier(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) &&
        value.ValueKind is JsonValueKind.String or JsonValueKind.Number ? value.ToString() : null;

    private static string? GetAuthorizationCode(JsonElement data) =>
        data.ValueKind == JsonValueKind.Object && data.TryGetProperty("authorization", out var authorization)
            ? GetOptionalString(authorization, "authorization_code") : null;

    private static DateTimeOffset? GetOptionalDate(JsonElement element, string property) =>
        GetOptionalString(element, property) is { } value && DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
