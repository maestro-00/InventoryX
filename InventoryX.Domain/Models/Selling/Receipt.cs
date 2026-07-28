using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling;

public class Receipt : BaseModel
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }
    public long SequenceNumber { get; set; }
    public required string Number { get; set; }
    public required string PayloadJson { get; set; }
}
