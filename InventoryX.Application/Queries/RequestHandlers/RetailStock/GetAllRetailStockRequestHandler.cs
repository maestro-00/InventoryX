using AutoMapper;
using InventoryX.Application.DTOs.RetailStock; 
using InventoryX.Application.Queries.Requests.RetailStock;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.RetailStock
{
    public class GetAllRetailStockRequestHandler(IRetailStockService service, IMapper mapper) : IRequestHandler<GetAllRetailStockRequest, ApiResponse>
    {
        private readonly IRetailStockService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetAllRetailStockRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetAllRetailStock() ?? throw new Exception("Failed to retrieve all retail stock");
                var retailStockDtos = _mapper.Map<IEnumerable<RetailStockDto>>(response);
                return new()
                {
                    Success = true,
                    Message = "Retrieved all retail stock successfully",
                    Body = retailStockDtos,
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
