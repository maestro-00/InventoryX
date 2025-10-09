using AutoMapper;
using InventoryX.Application.DTOs.Purchases;
using InventoryX.Application.Queries.Requests.Purchases;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.Purchases
{
    public class GetAllPurchaseRequestHandler(IPurchaseService service, IMapper mapper) : IRequestHandler<GetAllPurchaseRequest, ApiResponse>
    {
        private readonly IPurchaseService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetAllPurchaseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetAllPurchases() ?? throw new Exception("Failed to retrieve all purchases");
                var purchaseDtos = _mapper.Map<IEnumerable<GetPurchaseDto>>(response);
                return new()
                {
                    Success = true,
                    Message = "Retrieved all purchases successfully",
                    Body = purchaseDtos,
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
