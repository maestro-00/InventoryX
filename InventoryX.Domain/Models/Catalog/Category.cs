using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Catalog
{
    /// <summary>Evolved from InventoryItemType (research R4): tree with unique name per parent.</summary>
    public class Category : BaseModel
    {
        public required string Name { get; set; }
        public Guid? ParentId { get; set; }
        public Category? Parent { get; set; }
        /// <summary>Soft delete with recovery window (FR-060).</summary>
        public bool IsDeleted { get; set; }
    }
}
