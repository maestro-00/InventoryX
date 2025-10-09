using AutoMapper;
using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.Queries.Requests.InventoryItems;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.InventoryItems
{
    public class GetAllInventoryItemRequestHandler(IInventoryItemService service, IMapper mapper) : IRequestHandler<GetAllInventoryItemRequest, ApiResponse>
    {
        private readonly IInventoryItemService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetAllInventoryItemRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetAllInventoryItems() ?? throw new Exception("Failed to retrieve all inventory items");
                var inventoryItemDtos = _mapper.Map<IEnumerable<GetInventoryItemDto>>(response);
                return new()
                {
                    Success = true,
                    Message = "Retrieved all inventory items successfully",
                    Body = inventoryItemDtos,
                    StatusCode = StatusCodes.Status200OK
                };
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
