using AutoMapper;
using InventoryX.Application.Commands.Requests.InventoryItemTypes;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.InventoryItemTypes
{
    public class CreateInventoryItemTypeCommandHandler(IInventoryItemTypeService service, IMapper mapper) : IRequestHandler<CreateInventoryItemTypeCommand, ApiResponse>
    {
        private readonly IInventoryItemTypeService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(CreateInventoryItemTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var inventoryItemTypeEntity = _mapper.Map<InventoryItemType>(request.NewInventoryItemTypeDto);
                inventoryItemTypeEntity.Created_At = DateTime.UtcNow;
                var response = await _service.AddInventoryItemType(inventoryItemTypeEntity);
                if (response > 0)
                {
                    return new()
                    {
                        Id = response,
                        Success = true,
                        Message = "Inventory Item Type has been created successfully",
                        StatusCode = StatusCodes.Status201Created
                    };
                }
                throw new Exception("Failed to create Inventory Item Type");
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
