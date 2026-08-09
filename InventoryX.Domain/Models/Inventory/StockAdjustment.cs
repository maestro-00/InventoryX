using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory;

public enum StockAdjustmentStatus { Applied, AwaitingApproval, Rejected }

public sealed class AdjustmentReason : GlobalModel
{
    public Guid? TenantId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsSystem { get; set; }
}

public sealed class StockAdjustment : BaseModel
{
    public Guid LocationId { get; set; }
    public string ReasonCode { get; set; } = "Correction";
    public string? Note { get; set; }
    public string? RequestedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public StockAdjustmentStatus Status { get; set; }
    public ICollection<StockAdjustmentLine> Lines { get; set; } = [];
}

public sealed class StockAdjustmentLine : BaseModel
{
    public Guid StockAdjustmentId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal QtyDelta { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal? UnitCost { get; set; }
}
