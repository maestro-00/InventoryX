using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

/// <summary>Durable receipt record used to make payment-provider webhook delivery exactly-once.</summary>
public sealed class ProcessedWebhookEvent : GlobalModel
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
