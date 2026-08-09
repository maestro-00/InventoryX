using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling;
public enum CashMovementType { CashIn, CashOut }
public sealed class CashMovement : BaseModel
{
    public Guid ShiftId { get; set; }
    public CashMovementType Type { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public required string RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; }
}
