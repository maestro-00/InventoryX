using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Selling
{
    /// <summary>
    /// Finds the original sale for a return/exchange by receipt number or a
    /// free-text search term (FR-041 lookup).
    /// </summary>
    public class LookupSaleForReturnQuery : IRequest<List<SaleDto>>
    {
        public string? ReceiptNumber { get; init; }
        public string? Search { get; init; }
    }

    /// <summary>Current sellable availability of a product at a location.</summary>
    public class GetProductAvailabilityQuery : IRequest<ProductAvailabilityDto>
    {
        public Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public Guid? LocationId { get; init; }
    }
}
