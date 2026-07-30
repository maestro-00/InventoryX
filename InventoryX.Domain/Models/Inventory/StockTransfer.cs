using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory;

public enum StockTransferStatus { Draft, Dispatched, Received, ReceivedWithDiscrepancy, Cancelled }

public sealed class StockTransfer : BaseModel
{
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;
    public string? DiscrepancyReason { get; set; }
    public ICollection<StockTransferLine> Lines { get; set; } = [];
}

public sealed class StockTransferLine : BaseModel
{
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal QtyDispatched { get; set; }
    [Column(TypeName = "decimal(18,3)")]
    public decimal? QtyReceived { get; set; }
}
