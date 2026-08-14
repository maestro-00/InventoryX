using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Sync;

public enum RejectedOfflineSaleStatus
{
    Open,
    ReleasedForRetry,
    Reconciled,
}

/// <summary>
/// Immutable rejected offline sale retained for manager review (readiness item 3).
/// The original payload is never edited; resolution either releases the same
/// clientSaleId for retry or links an authoritative compensating reconciliation.
/// </summary>
public sealed class RejectedOfflineSale : BaseModel
{
    public Guid ClientSaleId { get; set; }
    public Guid RegisterId { get; set; }
    public Guid? ShiftId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string PayloadHash { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public RejectedOfflineSaleStatus Status { get; set; } = RejectedOfflineSaleStatus.Open;
    public Guid? LinkedReconciliationSaleId { get; set; }
    public string? ResolutionNote { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
