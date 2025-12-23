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
    public class CreateSaleCommandHandler(ISaleService service,IRetailStockService retailStockService, IMapper mapper) : IRequestHandler<CreateSaleCommand, ApiResponse>
    {
        private readonly ISaleService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var SaleEntity = _mapper.Map<Sale>(request.NewSaleDto);
                SaleEntity.Created_At = DateTime.UtcNow;
                var response = await _service.AddSale(SaleEntity);
                if (response > 0)
                {
                    var retailStock = await retailStockService.GetRetailStock("InventoryItemId", request.NewSaleDto.InventoryItemId);
                    if (retailStock == null) throw new CustomException("Failed to create sale.", StatusCodes.Status500InternalServerError);
                    retailStock.Quantity -= request.NewSaleDto.Quantity;
                    var updateResult = await retailStockService.UpdateRetailStock(retailStock);
                    if (updateResult > 0)
                    {
                        return new()
                        {
                            Id = response,
                            Success = true,
                            Message = "Sale has been created successfully"
                        };
                    }
                }
                throw new Exception("Failed to create sale");
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
