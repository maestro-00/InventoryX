using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Inventory
{
    public enum LocationKind { Shop, Warehouse, Both, Vehicle, Stall }

    public class Location : BaseModel
    {
        public required string Name { get; set; }
        public string? Address { get; set; }
        public LocationKind Kind { get; set; } = LocationKind.Shop;
        public bool IsActive { get; set; } = true;
        /// <summary>Soft delete with recovery window (FR-060).</summary>
        public bool IsDeleted { get; set; }
    }
}
