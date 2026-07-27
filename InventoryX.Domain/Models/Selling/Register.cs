using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    /// <summary>POS register at a location; count is plan-capped.</summary>
    public class Register : BaseModel
    {
        public Guid LocationId { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
