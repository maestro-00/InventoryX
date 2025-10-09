using AutoMapper;
using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Application.Queries.Requests.InventoryItemTypes;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.InventoryItemTypes
{
    public class GetAllInventoryItemTypeRequestHandler(IInventoryItemTypeService service, IMapper mapper) : IRequestHandler<GetAllInventoryItemTypeRequest, ApiResponse>
    {
        private readonly IInventoryItemTypeService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetAllInventoryItemTypeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetAllInventoryItemTypes() ?? throw new Exception("Failed to retrieve all inventory item types");
                var inventoryItemTypeDtos = _mapper.Map<IEnumerable<GetInventoryItemTypeDto>>(response);
                return new()
                {
                    Success = true,
                    Message = "Retrieved all inventory item types successfully",
                    Body = inventoryItemTypeDtos,
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
