using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Selling
{
    public class GetHeldSalesQuery : IRequest<List<SaleDto>>;

    public class GetHeldSaleQuery : IRequest<SaleDto>
    {
        public Guid Id { get; init; }
    }

    public class GetFavouritesLayoutQuery : IRequest<FavouritesLayoutDto>
    {
        public Guid RegisterId { get; init; }
    }
}
