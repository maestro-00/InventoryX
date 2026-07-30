using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling;

public enum ReceiptChannel { Email, Sms, Qr }

public class Receipt : BaseModel
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }
    public long SequenceNumber { get; set; }
    public required string Number { get; set; }
    public required string PayloadJson { get; set; }
}

public class ReceiptDeliveryLog : BaseModel
{
    public Guid ReceiptId { get; set; }
    public ReceiptChannel Channel { get; set; }
    public required string Destination { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DeliveredAt { get; set; }
}
