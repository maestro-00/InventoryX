using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Selling
{
    public record GetSalesQuery : PageRequest, IRequest<PagedResult<SaleDto>>
    {
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public Guid? LocationId { get; init; }
        public Guid? RegisterId { get; init; }
        public string? CashierId { get; init; }
        public string? Status { get; init; }
    }

    public class GetSaleQuery : IRequest<SaleDto>
    {
        public Guid Id { get; init; }
    }
}
