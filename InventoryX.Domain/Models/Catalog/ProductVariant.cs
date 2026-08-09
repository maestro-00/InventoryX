using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Catalog
{
    /// <summary>Stock is held at variant level when present (FR-021).</summary>
    public class ProductVariant : BaseModel
    {
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }
        /// <summary>JSON attribute values, e.g. {"Size":"M","Colour":"Red"}; must match the parent's attribute schema.</summary>
        public required string AttributeValues { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        /// <summary>Null = inherit from parent product.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? SellingPrice { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal? CostPrice { get; set; }
        public bool IsDeleted { get; set; }
    }
}
