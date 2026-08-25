using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Selling
{
    public class GetShiftsQuery : IRequest<List<ShiftDto>>
    {
        public Guid? RegisterId { get; init; }
        public string? Status { get; init; }
    }
}
