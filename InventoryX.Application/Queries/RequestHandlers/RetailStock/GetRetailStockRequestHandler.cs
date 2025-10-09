using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using InventoryX.Application.DTOs.RetailStock;
using InventoryX.Application.Queries.Requests.RetailStock;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Queries.RequestHandlers.RetailStock
{
    public class GetRetailStockRequestHandler(IRetailStockService service, IMapper mapper) : IRequestHandler<GetRetailStockRequest, ApiResponse>
    {
        private readonly IRetailStockService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetRetailStockRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetRetailStock(request.Id) ?? throw new Exception("Retail Stock does not exist");
                var RetailStockDto = _mapper.Map<RetailStockDto>(response);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Retrieved Retail Stock successfully",
                    Body = RetailStockDto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later."
                };

            }
        }
    }
}
