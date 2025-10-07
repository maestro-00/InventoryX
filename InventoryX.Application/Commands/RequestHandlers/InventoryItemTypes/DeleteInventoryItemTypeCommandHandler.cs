using AutoMapper;
using InventoryX.Application.Commands.Requests.InventoryItemTypes;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.InventoryItemTypes
{
    public class DeleteInventoryItemTypeCommandHandler(IInventoryItemTypeService service) : IRequestHandler<DeleteInventoryItemTypeCommand, ApiResponse>
    {
        private readonly IInventoryItemTypeService _service = service;

        public async Task<ApiResponse> Handle(DeleteInventoryItemTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id < 1)
                {
                    return new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Invalid inventory item type id passed"
                    };
                }
                
                var response = await _service.DeleteInventoryItemType(request.Id);
                if (response > 0)
                {
                    return new()
                    {
                        Success = true,
                        Message = "Inventory Item Type has been deleted successfully",
                        StatusCode = StatusCodes.Status202Accepted
                    };
                }
                throw new Exception("Failed to delete Inventory Item Type");
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later.",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
