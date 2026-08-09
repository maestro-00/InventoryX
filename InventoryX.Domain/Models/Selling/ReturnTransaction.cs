using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    /// <summary>Where returned stock goes: back on the shelf, or held aside.</summary>
    public enum ReturnDisposition { ToStock, Quarantine }

    /// <summary>Lifecycle of a return/exchange against an original sale.</summary>
    public enum ReturnStatus { PendingAuthorization, Completed, Rejected }

    /// <summary>Cycle 1 refund destinations; StoreCredit arrives with its balance ledger.</summary>
    public enum RefundTender { Original, Cash }

    /// <summary>
    /// A first-class immutable commercial transaction reversing part or all of an
    /// original sale. Original price + tax are applied automatically (FR-041); the
    /// authorization gate protects threshold/receiptless returns (423 until attached).
    /// </summary>
    public class ReturnTransaction : BaseModel
    {
        public Guid OriginalSaleId { get; set; }
        public Sale? OriginalSale { get; set; }

        /// <summary>For an exchange, the newly created sale settling the difference.</summary>
        public Guid? ExchangeSaleId { get; set; }

        public ReturnStatus Status { get; set; } = ReturnStatus.Completed;
        /// <summary>True when threshold/receiptless rules require a manager sign-off.</summary>
        public bool AuthorizationRequired { get; set; }
        /// <summary>Manager user id attached to authorize a gated return.</summary>
        public string? AuthorizedBy { get; set; }

        public RefundTender RefundTender { get; set; } = RefundTender.Original;
        [Column(TypeName = "decimal(18,4)")]
        public decimal RefundTotal { get; set; }

        public DateTime OccurredAt { get; set; }
        public string? Reason { get; set; }

        public ICollection<ReturnLine> Lines { get; set; } = [];
    }

    /// <summary>A returned line snapshotting the original commercial terms.</summary>
    public class ReturnLine : BaseModel
    {
        public Guid ReturnTransactionId { get; set; }
        public Guid SaleLineId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid? BatchId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Qty { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal OriginalUnitPrice { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal OriginalTaxAmount { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal LineRefund { get; set; }
        public ReturnDisposition Disposition { get; set; } = ReturnDisposition.ToStock;
    }
}
