using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Purchasing;

public enum PurchaseOrderStatus
{
    Draft,
    AwaitingApproval,
    Sent,
    PartiallyReceived,
    FullyReceived,
    Closed,
    Cancelled,
}

public enum PurchaseOrderOrigin { Manual, ReorderSuggestion, LowStockAlert }

public sealed class PurchaseOrder : BaseModel
{
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid DeliverToLocationId { get; set; }
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    public PurchaseOrderOrigin Origin { get; set; } = PurchaseOrderOrigin.Manual;
    public Guid? OriginReferenceId { get; set; }
    public DateTime? RequiredBy { get; set; }
    public string? Notes { get; set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? ClosedShortReason { get; private set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];

    [NotMapped]
    public decimal Total => Lines.Sum(line => line.OrderedQty * line.UnitCost);

    public void Submit(bool requiresApproval, DateTime now)
    {
        Ensure(PurchaseOrderStatus.Draft, "Only draft purchase orders can be submitted.");
        if (Lines.Count == 0) throw new InvalidOperationException("A purchase order requires at least one line.");
        Status = requiresApproval ? PurchaseOrderStatus.AwaitingApproval : PurchaseOrderStatus.Sent;
        if (!requiresApproval) SentAt = now;
    }

    public void Approve(string approverId, DateTime now)
    {
        Ensure(PurchaseOrderStatus.AwaitingApproval, "Only purchase orders awaiting approval can be approved.");
        Status = PurchaseOrderStatus.Sent;
        ApprovedBy = approverId;
        ApprovedAt = now;
        SentAt = now;
    }

    public void Reject()
    {
        Ensure(PurchaseOrderStatus.AwaitingApproval, "Only purchase orders awaiting approval can be rejected.");
        Status = PurchaseOrderStatus.Draft;
    }

    public void MarkPartiallyReceived() => TransitionReceipt(PurchaseOrderStatus.PartiallyReceived);
    public void MarkFullyReceived() => TransitionReceipt(PurchaseOrderStatus.FullyReceived);

    public void Close(DateTime now)
    {
        if (Status is not (PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.FullyReceived))
            throw new InvalidOperationException("Only received purchase orders can be closed.");
        Status = PurchaseOrderStatus.Closed;
        ClosedAt = now;
    }

    public void CloseShort(string reason, DateTime now)
    {
        Ensure(PurchaseOrderStatus.PartiallyReceived, "Only partially received purchase orders can be closed short.");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A close-short reason is required.");
        Status = PurchaseOrderStatus.Closed;
        ClosedShortReason = reason.Trim();
        ClosedAt = now;
    }

    public void Cancel(string reason, DateTime now)
    {
        if (Status is PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("A closed or cancelled purchase order cannot be cancelled.");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A cancellation reason is required.");
        Status = PurchaseOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        ClosedAt = now;
    }

    private void TransitionReceipt(PurchaseOrderStatus target)
    {
        if (Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
            throw new InvalidOperationException("Only sent purchase orders can receive goods.");
        Status = target;
    }

    private void Ensure(PurchaseOrderStatus expected, string message)
    {
        if (Status != expected) throw new InvalidOperationException(message);
    }
}

public sealed class PurchaseOrderLine : BaseModel
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public required string Description { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal OrderedQty { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal ReceivedQty { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal DamagedQty { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }
}
