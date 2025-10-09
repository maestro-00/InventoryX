using AutoMapper;
using InventoryX.Application.Commands.Requests.InventoryItemTypes;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.InventoryItemTypes
{
    public class UpdateInventoryItemTypeCommandHandler(IInventoryItemTypeService service, IMapper mapper) : IRequestHandler<UpdateInventoryItemTypeCommand, ApiResponse>
    {
        private readonly IInventoryItemTypeService _service = service;
        private readonly IMapper _mapper = mapper;

        public async Task<ApiResponse> Handle(UpdateInventoryItemTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var inventoryItemTypeEntity = _mapper.Map<InventoryItemType>(request.InventoryItemTypeDto);
                inventoryItemTypeEntity.Id = request.Id;
                inventoryItemTypeEntity.Updated_At = DateTime.UtcNow;
                var response = await _service.UpdateInventoryItemType(inventoryItemTypeEntity);
                if (response > 0)
                {
                    return new()
                    {
                        Id = inventoryItemTypeEntity.Id,
                        Success = true,
                        Message = "Inventory Item Type has been updated successfully",
                        StatusCode = StatusCodes.Status202Accepted
                    };
                }
                throw new Exception("Failed to update Inventory Item Type");
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
