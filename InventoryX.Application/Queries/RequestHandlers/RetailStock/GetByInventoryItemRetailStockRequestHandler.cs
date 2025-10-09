using AutoMapper;
using InventoryX.Application.DTOs.RetailStock;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.RetailStock;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.RetailStock
{
    public class GetByInventoryItemRetailStockRequestHandler(IRetailStockService service, IMapper mapper) : IRequestHandler<GetByInventoryItemRetailStockRequest, ApiResponse>
    {
        private readonly IMapper _mapper = mapper;
        private readonly IRetailStockService _service = service;
        public async Task<ApiResponse> Handle(GetByInventoryItemRetailStockRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id < 1) throw new CustomException("Invalid inventory item id passed.", StatusCodes.Status400BadRequest);
                var response = await _service.GetRetailStock("InventoryItemId", request.Id) ?? throw new Exception("Retail Stock does not exist");
                var retailStockDto = _mapper.Map<RetailStockDto>(response);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Retrieved Retail Stock successfully",
                    Body = retailStockDto,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (CustomException ce)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ce.Message ?? "Something went wrong. Try again later.",
                    StatusCode = ce.StatusCode
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
