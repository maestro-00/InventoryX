using System.ComponentModel.DataAnnotations;
using InventoryX.Application.DTOs.Sales;

namespace InventoryX.Application.DTOs.SaleGroups;

public class SaleGroupCommandDto
{
    public string? CustomerName { get; set; }
    public string? PaymentMethod { get; set; }
    [Required]
    public required decimal TotalAmount { get; set; }
    public IEnumerable<SaleCommandDto> Sales { get; set; }
}
