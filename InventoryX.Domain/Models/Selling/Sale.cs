using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    public enum SaleStatus { Held, Completed, PartiallyReturned, Returned, Voided }

    public enum SaleChannel { Pos, Api }

    /// <summary>
    /// Evolved from SaleGroup (research R4); the old Sale rows became SaleLine.
    /// ClientSaleId is the offline idempotency key (research R6).
    /// </summary>
    public class Sale : BaseModel
    {
        public Guid LocationId { get; set; }
        public Guid RegisterId { get; set; }
        public Guid ShiftId { get; set; }
        public required string CashierId { get; set; }
        /// <summary>Client-generated UUID; unique per tenant for idempotent replays.</summary>
        public Guid ClientSaleId { get; set; }
        public SaleChannel Channel { get; set; } = SaleChannel.Pos;
        public bool OfflineOrigin { get; set; }
        /// <summary>Set when an offline ingest conflicted with concurrent stock (FR-046).</summary>
        public bool StockConflictFlag { get; set; }
        /// <summary>Reserved extension point — customers arrive in a later cycle.</summary>
        public Guid? CustomerId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Subtotal { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal DiscountTotal { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal TaxTotal { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal GrandTotal { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal ChangeGiven { get; set; }

        public SaleStatus Status { get; set; } = SaleStatus.Completed;
        public DateTime OccurredAt { get; set; }
        public string? VoidedBy { get; set; }
        public DateTime? VoidedAt { get; set; }

        public ICollection<SaleLine> Lines { get; set; } = [];
        public ICollection<SalePayment> Payments { get; set; } = [];
    }
}
