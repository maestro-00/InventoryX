using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Purchasing;

public sealed class SupplierInvoice : BaseModel
{
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public bool HasPriceVariance { get; set; }
    public string? Notes { get; set; }
    public List<SupplierInvoiceLine> Lines { get; set; } = [];

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount => Lines.Sum(l => l.Qty * l.UnitPrice);
}

public sealed class SupplierInvoiceLine : BaseModel
{
    public Guid SupplierInvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal Qty { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal? OrderedUnitCost { get; set; }
    public bool HasVariance => OrderedUnitCost.HasValue && Math.Abs(UnitPrice - OrderedUnitCost.Value) > 0.0001m;
}
