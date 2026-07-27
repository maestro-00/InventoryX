using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    public enum ShiftStatus { Open, Closed }

    /// <summary>
    /// Register operating session: opening float through counted close with
    /// variance (FR-042). Full cash management lands with US6.
    /// </summary>
    public class Shift : BaseModel
    {
        public Guid RegisterId { get; set; }
        public Register? Register { get; set; }
        public required string OpenedBy { get; set; }
        public DateTime OpenedAt { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal OpeningFloat { get; set; }
        public string? ClosedBy { get; set; }
        public DateTime? ClosedAt { get; set; }
        /// <summary>Counted drawer at close — close is rejected without it (FR-042).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ClosingCounted { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExpectedCash { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? Variance { get; set; }
        public bool VarianceFlagged { get; set; }
        public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    }
}
