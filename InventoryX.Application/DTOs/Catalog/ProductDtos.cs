namespace InventoryX.Application.DTOs.Catalog
{
    public class ProductVariantDto
    {
        public Guid Id { get; init; }
        public Dictionary<string, string> AttributeValues { get; init; } = [];
        public string? Sku { get; init; }
        public string? Barcode { get; init; }
        public decimal? SellingPrice { get; init; }
        public decimal? CostPrice { get; init; }
    }

    public class ProductDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Sku { get; init; }
        public string? Barcode { get; init; }
        public Guid? CategoryId { get; init; }
        public string UnitOfMeasure { get; init; } = "Each";
        public bool AllowFractional { get; init; }
        public decimal SellingPrice { get; init; }
        /// <summary>Null when the caller lacks ViewProfit (FR-050).</summary>
        public decimal? CostPrice { get; init; }
        public string? TaxTreatmentCode { get; init; }
        public string TrackingMode { get; init; } = "Simple";
        public string Status { get; init; } = "Active";
        public decimal? ReorderPoint { get; init; }
        public decimal? ReorderQuantity { get; init; }
        public int? LeadTimeDays { get; init; }
        public List<string> VariantAttributes { get; init; } = [];
        public List<ProductVariantDto> Variants { get; init; } = [];
    }

    public class CategoryDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid? ParentId { get; init; }
        public List<CategoryDto> Children { get; init; } = [];
    }

    public class TaxTreatmentDto
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string CountryCode { get; init; } = "GH";
        public string ComponentsJson { get; init; } = "[]";
    }
}
