using AutoMapper;
using InventoryX.Application.Commands.Requests.RetailStock;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.RetailStocks
{
    public class UpdateRetailStockCommandHandler(IRetailStockService service, IInventoryItemService inventoryItemService, IMapper mapper) : IRequestHandler<UpdateRetailStockCommand, ApiResponse>
    {
        private readonly IRetailStockService _service = service;
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(UpdateRetailStockCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.RetailStock == null)throw new CustomException("Retail stock is required", StatusCodes.Status400BadRequest);
             
                RetailStock updatedRetailStock = _mapper.Map<RetailStock>(request.RetailStock);
                var inventoryItem = await _inventoryItemService.GetInventoryItem(updatedRetailStock.InventoryItemId);
                if (inventoryItem == null) throw new CustomException("Inventory item not found", StatusCodes.Status404NotFound);
                
                var oldRetailStock = await _service.GetRetailStock("InventoryItemId", request.RetailStock.InventoryItemId); 
                if (oldRetailStock == null) throw new CustomException("Retail stock not found", StatusCodes.Status404NotFound);
        
                if (updatedRetailStock.Quantity > inventoryItem.TotalAmount) throw new CustomException("Retail Stock quantity cannot be greater than total inventory item amount", StatusCodes.Status400BadRequest);
                updatedRetailStock.Id = oldRetailStock.Id;
                updatedRetailStock.Updated_At = DateTime.UtcNow; 
                var response = await _service.UpdateRetailStock(updatedRetailStock);
                if (response <= 0) throw new CustomException("Failed to update Retail Stock", StatusCodes.Status500InternalServerError);

                return new() { Id = updatedRetailStock.Id, Message = "Retail Stock updated successfully.", Success = true, StatusCode = StatusCodes.Status202Accepted};

            }
            catch (CustomException ce)
            {
                return new()
                {
                    Success = false,
                    Message = ce.Message,
                    StatusCode = ce.StatusCode
                };
            }
            catch (Exception e)
            {
                return new()
                {
                    Success = false,
                    Message = e.Message ?? "Something went wrong. Try again later.",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
