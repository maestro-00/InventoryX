using InventoryX.Application.DTOs.SaleGroups;
using MediatR;

namespace InventoryX.Application.Commands.Requests.SaleGroups;

public class CreateSaleGroupCommand : IRequest<ApiResponse>
{
    public SaleGroupCommandDto SaleGroupCommandDto { get; set; }
}
