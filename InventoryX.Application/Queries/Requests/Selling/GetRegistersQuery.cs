using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Selling
{
    public class GetRegistersQuery : IRequest<List<RegisterDto>>
    {
        public Guid? LocationId { get; init; }
    }
}
