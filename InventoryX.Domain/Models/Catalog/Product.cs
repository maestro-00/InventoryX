using System.ComponentModel.DataAnnotations.Schema;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Catalog
{
    /// <summary>Serial, Bundle, Recipe, Asset, Consignment, NonStock reserved for later cycles.</summary>
    public enum TrackingMode { Simple, Variant, Batch, Serial, Bundle, Recipe, Asset, Consignment, NonStock }

    public enum ProductStatus { Active, Inactive, Discontinued }

    public enum UnitOfMeasure { Each, Box, Kg, G, Litre, Ml, Metre, Hour }

    /// <summary>Evolved from InventoryItem (research R4).</summary>
    public class Product : BaseModel
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }
        /// <summary>JSON array of tags.</summary>
        public string? Tags { get; set; }

        public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Each;
        public bool AllowFractional { get; set; }

        /// <summary>Maintained by the tenant's valuation method (weighted average in Cycle 1).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostPrice { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal SellingPrice { get; set; }
        public Guid? TaxTreatmentId { get; set; }
        public TaxTreatment? TaxTreatment { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? ReorderPoint { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal? ReorderQuantity { get; set; }
        public int? LeadTimeDays { get; set; }

        /// <summary>JSON array of blob refs.</summary>
        public string? Photos { get; set; }
        /// <summary>JSON per-tenant custom field values.</summary>
        public string? CustomFields { get; set; }
        /// <summary>JSON attribute schema for variants, e.g. ["Size","Colour"].</summary>
        public string? VariantAttributes { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public TrackingMode TrackingMode { get; set; } = TrackingMode.Simple;
        /// <summary>Sample-data flag so demo records can be removed in one action (FR-019).</summary>
        public bool IsSampleData { get; set; }
        /// <summary>Soft delete with recovery window (FR-060).</summary>
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? RecoveryExpiresAt { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = [];
    }
}
