using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using InventoryX.Application.Commands.Requests.Sales;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.Sales
{
    public class UpdateSaleCommandHandler(ISaleService service, IInventoryItemService inventoryItemService, IMapper mapper, IRetailStockService retailStockService) : IRequestHandler<UpdateSaleCommand, ApiResponse>
    {
        private readonly ISaleService _service = service;
        private readonly IMapper _mapper = mapper;

        public async Task<ApiResponse> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var oldSale = await _service.GetSale(request.Id);
                if (oldSale == null) throw new CustomException("Sale not found.", StatusCodes.Status400BadRequest);
                var SaleEntity = _mapper.Map<Sale>(request.SaleDto);
                SaleEntity.Id = request.Id;
                SaleEntity.Updated_At = DateTime.UtcNow;
                var response = await _service.UpdateSale(SaleEntity);
                if (response > 0)
                {
                    var retailStock = await retailStockService.GetRetailStock("InventoryItemId", request.SaleDto.InventoryItemId);
                    if (retailStock == null) throw new CustomException("Failed to update sale.", StatusCodes.Status500InternalServerError);
                    retailStock.Quantity -= (SaleEntity.Quantity - oldSale.Quantity);
                    var updateRetailStock = await retailStockService.UpdateRetailStock(retailStock);
                    if(updateRetailStock <= 0) throw new Exception("Failed to update sale");

                    SaleEntity.InventoryItem.TotalAmount -= request.SaleDto.Quantity;

                    var updateInventoryResult = await inventoryItemService.UpdateInventoryItem(SaleEntity.InventoryItem);
                    if (updateInventoryResult > 0)
                    {
                        return new()
                        {
                            Id = response,
                            Success = true,
                            Message = "Sale has been updated successfully",
                            StatusCode = StatusCodes.Status200OK
                        };
                    }
                }
                throw new Exception("Failed to update Sale");
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later."
                };
            }
        }
    }
}
