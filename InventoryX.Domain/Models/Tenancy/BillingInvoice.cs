using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Tenancy;

public enum BillingInvoiceStatus { Paid, Pending, Failed, Voided }

public sealed class BillingInvoice : BaseModel
{
    public Guid SubscriptionId { get; set; }
    public string Number { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,4)")] public decimal Amount { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal TaxAmount { get; set; }
    public string Currency { get; set; } = "GHS";
    public BillingInvoiceStatus Status { get; set; } = BillingInvoiceStatus.Paid;
    public string? PaymentReference { get; set; }
    public string? PdfContent { get; set; }
    public string? EmailedTo { get; set; }
    public DateTime? EmailedAt { get; set; }
}
