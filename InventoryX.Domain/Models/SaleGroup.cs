using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models;

public class SaleGroup : BaseModel
{
    public string? CustomerName { get; set; }
    public string? PaymentMethod { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
}
