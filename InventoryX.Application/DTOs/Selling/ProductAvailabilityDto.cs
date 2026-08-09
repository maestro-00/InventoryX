namespace InventoryX.Application.DTOs.Selling
{
    public class ProductAvailabilityDto
    {
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public Guid? LocationId { get; init; }
        public decimal QtyOnHand { get; init; }
        public decimal QtyAvailable { get; init; }
        public bool InStock { get; init; }
    }
}
