using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum PaystackEventStatus { Received, Processed, Failed }
public enum PaystackSignatureMode { SigningSecret, LegacySecret }

/// <summary>Durable signed-event ingress before asynchronous reconciliation.</summary>
public sealed class PaystackEventInbox : GlobalModel
{
    public Guid? TenantId { get; set; }
    public string? ProviderEventId { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string EventType { get; set; } = string.Empty;
    public PaystackSignatureMode SignatureMode { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public PaystackEventStatus Status { get; set; } = PaystackEventStatus.Received;
    public string? SafeErrorSummary { get; set; }
}
