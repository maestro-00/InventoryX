using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Purchasing;

public sealed class GoodsReceipt : BaseModel
{
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid LocationId { get; set; }
    public required string ReceiptNumber { get; set; }
    public string? ReceivedBy { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; } = [];
}

public sealed class GoodsReceiptLine : BaseModel
{
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal QtyReceived { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal QtyDamaged { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }
    [NotMapped]
    public decimal QtyAccepted => QtyReceived - QtyDamaged;
}
