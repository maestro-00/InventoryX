using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Purchasing;

public sealed class SupplierProduct : BaseModel
{
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public string? SupplierCode { get; set; }
    [Column(TypeName = "decimal(18,4)")]
    public decimal LastPrice { get; set; }
    public DateTime PriceUpdatedAt { get; set; } = DateTime.UtcNow;
}
