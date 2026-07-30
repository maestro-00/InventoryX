using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

/// <summary>Durable integration event; a worker retries delivery without losing billing state changes.</summary>
public sealed class OutboxMessage : BaseModel
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
