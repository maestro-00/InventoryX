using AutoMapper;
using InventoryX.Application.DTOs.Purchases;
using InventoryX.Application.Queries.Requests.Purchases;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.Purchases
{
    public class GetPurchaseRequestHandler(IPurchaseService service, IMapper mapper) : IRequestHandler<GetPurchaseRequest, ApiResponse>
    {
        private readonly IPurchaseService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetPurchaseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id < 1)
                {
                    return new ApiResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Invalid Purchase Id passed"
                    };
                }

                var response = await _service.GetPurchase(request.Id);
                if (response == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Purchase not found",
                        Success = false,
                    };
                }
                var purchaseDto = _mapper.Map<GetPurchaseDto>(response);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Retrieved Purchase successfully",
                    Body = purchaseDto,
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
