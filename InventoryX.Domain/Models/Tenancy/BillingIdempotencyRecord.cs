using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

/// <summary>A completed owner mutation, keyed per tenant and retained for safe retries.</summary>
public sealed class BillingIdempotencyRecord : BaseModel
{
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
