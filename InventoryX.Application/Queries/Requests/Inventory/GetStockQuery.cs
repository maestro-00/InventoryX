using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Inventory;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Inventory
{
    public record GetStockQuery : PageRequest, IRequest<PagedResult<StockLevelDto>>
    {
        public Guid? LocationId { get; init; }
        public Guid? ProductId { get; init; }
        public Guid? CategoryId { get; init; }
        public bool? BelowReorder { get; init; }
        /// <summary>"product" → business-wide rollup across locations (FR-022).</summary>
        public string? GroupBy { get; init; }
        public bool IncludeCost { get; init; } = true;
    }

    public class GetLocationsQuery : IRequest<List<LocationDto>>;

    public record GetStockMovementsQuery : PageRequest, IRequest<PagedResult<StockMovementDto>>
    {
        public Guid? ProductId { get; init; }
        public Guid? LocationId { get; init; }
        public string? Type { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public string? UserId { get; init; }
    }
}
