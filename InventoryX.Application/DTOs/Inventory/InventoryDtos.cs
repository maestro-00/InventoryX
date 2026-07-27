namespace InventoryX.Application.DTOs.Inventory
{
    public class LocationDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Address { get; init; }
        public string Kind { get; init; } = "Shop";
        public bool IsActive { get; init; }
    }

    public class StockLevelDto
    {
        public Guid ProductId { get; init; }
        public string? ProductName { get; init; }
        public Guid? VariantId { get; init; }
        public Guid? LocationId { get; init; }
        public Guid? BatchId { get; init; }
        public decimal QtyOnHand { get; init; }
        public decimal QtyInTransit { get; init; }
        public decimal QtyQuarantine { get; init; }
        /// <summary>Null without ViewProfit (FR-050).</summary>
        public decimal? AvgUnitCost { get; init; }
    }

    public class StockMovementDto
    {
        public Guid Id { get; init; }
        public string Type { get; init; } = string.Empty;
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public Guid? BatchId { get; init; }
        public Guid LocationId { get; init; }
        public decimal QtyDelta { get; init; }
        public string? ReasonCode { get; init; }
        public string? Note { get; init; }
        public string? UserId { get; init; }
        public DateTime OccurredAt { get; init; }
    }
}
