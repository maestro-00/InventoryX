using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory;

public sealed class Batch : BaseModel
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? SupplierId { get; set; }
    public required string BatchNumber { get; set; }
    public DateTime? ManufacturedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }
}
