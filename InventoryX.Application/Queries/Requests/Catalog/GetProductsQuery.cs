using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.DTOs.Common;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Catalog
{
    public record GetProductsQuery : PageRequest, IRequest<PagedResult<ProductDto>>
    {
        /// <summary>Name/SKU/barcode search, typo-tolerant (US2 T055 hardens this).</summary>
        public string? Search { get; init; }
        public Guid? CategoryId { get; init; }
        public string? Status { get; init; }
        public string? TrackingMode { get; init; }
        public bool? BelowReorderPoint { get; init; }
        public bool IncludeCost { get; init; } = true;
    }

    public class GetProductQuery : IRequest<ProductDto>
    {
        public Guid Id { get; init; }
        public bool IncludeCost { get; init; } = true;
    }

    public class GetProductByBarcodeQuery : IRequest<ProductDto>
    {
        public required string Barcode { get; init; }
        public bool IncludeCost { get; init; } = true;
    }

    public class GetCategoriesQuery : IRequest<List<CategoryDto>>;

    public class GetTaxTreatmentsQuery : IRequest<List<TaxTreatmentDto>>;
}
