using InventoryX.Application.DTOs.Sales;

namespace InventoryX.Application.DTOs.SaleGroups;

public class SaleGroupDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public IEnumerable<GetSaleDto> Sales { get; set; }
}
