using System.Text.Json;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Infrastructure.BackgroundJobs;

public sealed record EmailOutboxPayload(
    string To,
    string Subject,
    string HtmlBody,
    string? FileName = null,
    string? ContentType = null,
    byte[]? Content = null);

public static class EmailOutbox
{
    public const string HtmlType = "email.html";
    public const string AttachmentType = "email.attachment";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage Html(
        Guid tenantId,
        string idempotencyKey,
        string to,
        string subject,
        string htmlBody,
        DateTime occurredAt) => Create(
        tenantId, HtmlType, idempotencyKey,
        new EmailOutboxPayload(to, subject, htmlBody), occurredAt);

    public static OutboxMessage Attachment(
        Guid tenantId,
        string idempotencyKey,
        string to,
        string subject,
        string htmlBody,
        string fileName,
        string contentType,
        byte[] content,
        DateTime occurredAt) => Create(
        tenantId, AttachmentType, idempotencyKey,
        new EmailOutboxPayload(to, subject, htmlBody, fileName, contentType, content), occurredAt);

    public static EmailOutboxPayload Deserialize(OutboxMessage message) =>
        JsonSerializer.Deserialize<EmailOutboxPayload>(message.Payload, SerializerOptions)
        ?? throw new InvalidOperationException("Email outbox payload is empty.");

    private static OutboxMessage Create(
        Guid tenantId,
        string type,
        string idempotencyKey,
        EmailOutboxPayload payload,
        DateTime occurredAt) => new()
    {
        TenantId = tenantId,
        Type = type,
        IdempotencyKey = idempotencyKey,
        Payload = JsonSerializer.Serialize(payload, SerializerOptions),
        OccurredAt = occurredAt,
        AvailableAt = occurredAt,
    };
}
