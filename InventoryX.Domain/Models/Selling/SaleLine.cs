using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    /// <summary>A sold line with persisted price + Ghana tax snapshot (FR-040, research R11).</summary>
    public class SaleLine : BaseModel
    {
        public Guid SaleId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        /// <summary>FEFO-assigned for batch-tracked products (US7).</summary>
        public Guid? BatchId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Qty { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }
        public bool PriceOverridden { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal LineDiscount { get; set; }
        /// <summary>User who authorized a discount above the cashier's role cap.</summary>
        public string? DiscountAuthorizedBy { get; set; }
        /// <summary>JSON snapshot of tax components applied to this line.</summary>
        public string TaxComponents { get; set; } = "[]";
        [Column(TypeName = "decimal(18,4)")]
        public decimal TaxAmount { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal LineTotal { get; set; }
        public string? Note { get; set; }
        /// <summary>Quantity already returned against this line.</summary>
        [Column(TypeName = "decimal(18,3)")]
        public decimal QtyReturned { get; set; }

        /// <summary>Product name snapshot for receipts/history.</summary>
        public string ProductName { get; set; } = string.Empty;
    }
}
