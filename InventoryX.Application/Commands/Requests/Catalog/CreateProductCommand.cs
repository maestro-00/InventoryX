using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Catalog
{
    /// <summary>Create product (FR-020); 402 above plan MaxProducts.</summary>
    public class CreateProductCommand : IRequest<ProductDto>, IPlanLimitedCommand
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public string? Sku { get; init; }
        public string? Barcode { get; init; }
        public Guid? CategoryId { get; init; }
        public string UnitOfMeasure { get; init; } = "Each";
        public bool AllowFractional { get; init; }
        public decimal SellingPrice { get; init; }
        public decimal CostPrice { get; init; }
        public string? TaxTreatmentCode { get; init; }
        public string TrackingMode { get; init; } = "Simple";
        public List<string>? VariantAttributes { get; init; }
        public decimal? ReorderPoint { get; init; }
        public decimal? ReorderQuantity { get; init; }
        public int? LeadTimeDays { get; init; }
        public bool IsSampleData { get; init; }

        public UsageMetric Metric => UsageMetric.Products;
    }

    public class UpdateProductCommand : IRequest<ProductDto>, ITenantWriteCommand, IAuditedCommand
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Sku { get; init; }
        public string? Barcode { get; init; }
        public Guid? CategoryId { get; init; }
        public decimal? SellingPrice { get; init; }
        public decimal? CostPrice { get; init; }
        public string? TaxTreatmentCode { get; init; }
        public string? Status { get; init; }
        public decimal? ReorderPoint { get; init; }
        public decimal? ReorderQuantity { get; init; }
        public int? LeadTimeDays { get; init; }

        public string AuditAction => "product.update";
        public string AuditEntityType => "Product";
        public string AuditEntityId => Id.ToString();
    }

    public class VariantInputDto
    {
        public Dictionary<string, string> AttributeValues { get; init; } = [];
        public string? Sku { get; init; }
        public string? Barcode { get; init; }
        public decimal? SellingPrice { get; init; }
        public decimal? CostPrice { get; init; }
    }

    /// <summary>Add variants with attribute values matching the parent's schema (FR-021).</summary>
    public class AddProductVariantsCommand : IRequest<ProductDto>, ITenantWriteCommand
    {
        public Guid ProductId { get; init; }
        public List<VariantInputDto> Variants { get; init; } = [];
    }

    public class CreateCategoryCommand : IRequest<CategoryDto>, ITenantWriteCommand
    {
        public required string Name { get; init; }
        public Guid? ParentId { get; init; }
    }

    public class UpdateCategoryCommand : IRequest<CategoryDto>, ITenantWriteCommand
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public Guid? ParentId { get; init; }
    }

    public class DeleteCategoryCommand : IRequest<bool>, ITenantWriteCommand
    {
        public Guid Id { get; init; }
    }
}
