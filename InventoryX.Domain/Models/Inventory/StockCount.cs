using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory;

public enum StockCountScope { Full, Cycle, Spot }
public enum StockCountStatus { Open, Counting, AwaitingApproval, Approved, Rejected }

public sealed class StockCount : BaseModel
{
    public Guid LocationId { get; set; }
    public StockCountScope Scope { get; set; }
    public StockCountStatus Status { get; set; } = StockCountStatus.Open;
    public string? CountedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public ICollection<StockCountLine> Lines { get; set; } = [];
}

public sealed class StockCountLine : BaseModel
{
    public Guid StockCountId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal ExpectedQty { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal? CountedQty { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal VarianceQty { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal VarianceValue { get; set; }
}
