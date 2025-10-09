using AutoMapper;
using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.Queries.Requests.InventoryItems;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.InventoryItems
{
    public class GetInventoryItemRequestHandler(IInventoryItemService service, IMapper mapper) : IRequestHandler<GetInventoryItemRequest, ApiResponse>
    {
        private readonly IInventoryItemService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetInventoryItemRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id == 0)
                {
                    return new ApiResponse
                    {
                        Message = "Invalid Item Id passed.",
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false
                    };
                }
                var response = await _service.GetInventoryItem(request.Id);
                if (response == null)
                {
                    return new ApiResponse
                    {
                        Message = "Item not found",
                        StatusCode = StatusCodes.Status404NotFound,
                        Success = false
                    };
                }
                var inventoryItemDto = _mapper.Map<GetInventoryItemDto>(response);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Retrieved inventory items successfully",
                    Body = inventoryItemDto,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later.",
                    StatusCode = StatusCodes.Status500InternalServerError
                };

            }
        }
    }
}
