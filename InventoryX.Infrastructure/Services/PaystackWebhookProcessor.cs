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
public sealed class PaystackWebhookProcessor(AppDbContext context, IOptions<PaystackOptions> options, IBillingInvoiceService invoiceService)
{
    private readonly string _secretKey = options.Value.SecretKey;

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

    public async Task<PaystackWebhookResult> ProcessAsync(string payload, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = GetRequiredString(root, "event");
        var data = root.TryGetProperty("data", out var parsedData) ? parsedData : default;
        var reference = GetOptionalString(data, "reference");
        var eventId = GetOptionalString(root, "id") ?? GetOptionalString(data, "id")
            ?? (!string.IsNullOrWhiteSpace(reference) ? $"{eventType}:{reference}" : throw new InvalidOperationException("Paystack webhook has no event identifier."));

        if (await context.ProcessedWebhookEvents.AnyAsync(item => item.EventId == eventId, cancellationToken))
            return new PaystackWebhookResult(true, eventType, reference);

        var occurredAt = GetOptionalDate(data, "paid_at") ?? GetOptionalDate(data, "created_at");
        if (occurredAt is not null && occurredAt.Value < DateTimeOffset.UtcNow.AddMinutes(-15))
            throw new InvalidOperationException("Paystack webhook is outside the 15-minute replay window.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (await context.ProcessedWebhookEvents.AnyAsync(item => item.EventId == eventId, cancellationToken))
            return new PaystackWebhookResult(true, eventType, reference);

        context.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            EventId = eventId,
            EventType = eventType,
            Reference = reference,
            Payload = payload,
            ProcessedAt = DateTime.UtcNow,
        });

        if (eventType is "charge.success" or "charge.failed")
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("Paystack charge webhook has no reference.");
            var subscription = await context.Subscriptions.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.PaymentMethodRef == reference, cancellationToken);
            if (subscription is not null)
            {
                if (eventType == "charge.success")
                {
                    SubscriptionStateMachine.RecordChargeSuccess(subscription, DateTime.UtcNow);
                    subscription.PaymentMethodRef = GetAuthorizationCode(data) ?? subscription.PaymentMethodRef;
                    await invoiceService.GenerateAndEmailAsync(subscription, reference, cancellationToken);
                }
                else
                    SubscriptionStateMachine.RecordChargeFailure(subscription, DateTime.UtcNow);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PaystackWebhookResult(false, eventType, reference);
    }

    private static string GetRequiredString(JsonElement element, string property) =>
        GetOptionalString(element, property) ?? throw new InvalidOperationException($"Paystack webhook has no {property}.");

    private static string? GetOptionalString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static string? GetAuthorizationCode(JsonElement data) =>
        data.ValueKind == JsonValueKind.Object && data.TryGetProperty("authorization", out var authorization)
            ? GetOptionalString(authorization, "authorization_code") : null;

    private static DateTimeOffset? GetOptionalDate(JsonElement element, string property) =>
        GetOptionalString(element, property) is { } value && DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
