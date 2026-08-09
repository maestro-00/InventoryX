using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory
{
    public enum MovementType
    {
        Receipt, Sale, ReturnIn, ReturnToSupplier, TransferOut, TransferIn,
        Adjustment, Consumption, CountCorrection,
    }

    /// <summary>
    /// Append-only stock ledger (research R5). Corrections are compensating
    /// entries; originals are immutable (FR-024).
    /// </summary>
    public class StockMovement : BaseModel
    {
        public MovementType Type { get; set; }
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid? BatchId { get; set; }
        public Guid LocationId { get; set; }
        /// <summary>Signed quantity delta (negative = stock out).</summary>
        [Column(TypeName = "decimal(18,3)")]
        public decimal QtyDelta { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }
        public string? UserId { get; set; }
        /// <summary>Adjustment/consumption reason code, e.g. Correction, Damage.</summary>
        public string? ReasonCode { get; set; }
        public string? Note { get; set; }
        /// <summary>Links the legs of a transfer, the lines of a sale, etc.</summary>
        public Guid? CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
