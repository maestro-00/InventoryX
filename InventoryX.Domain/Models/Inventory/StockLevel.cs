using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory
{
    /// <summary>
    /// Evolved from RetailStock (research R4/R5): maintained projection of the
    /// movement ledger, unique per (Product, Variant, Location, Batch).
    /// </summary>
    public class StockLevel : BaseModel
    {
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid LocationId { get; set; }
        public Guid? BatchId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QtyOnHand { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal QtyInTransit { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal QtyQuarantine { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal AvgUnitCost { get; set; }
    }
}
