using InventoryX.Application.DTOs.InventoryItemTypes;
using MediatR;

namespace InventoryX.Application.Commands.Requests.InventoryItemTypes
{
    public class CreateInventoryItemTypeCommand : IRequest<ApiResponse>
    {
        public required InventoryItemTypeCommandDto NewInventoryItemTypeDto { get; set; }
    }
}
