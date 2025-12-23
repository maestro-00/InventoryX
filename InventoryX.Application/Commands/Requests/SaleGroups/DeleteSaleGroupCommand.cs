using MediatR;

namespace InventoryX.Application.Commands.Requests.SaleGroups;

public class DeleteSaleGroupCommand : IRequest<ApiResponse>
{
    public required int Id { get; set; }
}
