using AutoMapper;
using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Application.Queries.Requests.InventoryItemTypes;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.InventoryItemTypes
{
    public class GetInventoryItemTypeRequestHandler(IInventoryItemTypeService service, IMapper mapper) : IRequestHandler<GetInventoryItemTypeRequest, ApiResponse>
    {
        private readonly IInventoryItemTypeService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetInventoryItemTypeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id < 1)
                {
                    return new ApiResponse
                    {
                        Message = "Inventory Item Type Id is invalid.",
                        Success = false,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                var response = await _service.GetInventoryItemType(request.Id);
                if (response is null)
                {
                    return new ApiResponse
                    {
                        Message = "Inventory Item Type not found.",
                        Success = false,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }
                var inventoryItemTypeDto = _mapper.Map<GetInventoryItemTypeDto>(response);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Retrieved inventory item types successfully",
                    Body = inventoryItemTypeDto,
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
