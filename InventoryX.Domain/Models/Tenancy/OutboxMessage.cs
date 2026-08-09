using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

/// <summary>Durable integration event; a worker retries delivery without losing billing state changes.</summary>
public sealed class OutboxMessage : BaseModel
{
    public string Type { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public string? ClaimedBy { get; set; }
    public DateTime? ClaimExpiresAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
