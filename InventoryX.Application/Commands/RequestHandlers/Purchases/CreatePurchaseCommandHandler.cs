using AutoMapper;
using InventoryX.Application.Commands.Requests.Purchases;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.Purchases
{
    public class CreatePurchaseCommandHandler(IPurchaseService service, IMapper mapper) : IRequestHandler<CreatePurchaseCommand, ApiResponse>
    {
        private readonly IPurchaseService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(CreatePurchaseCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var purchaseEntity = _mapper.Map<Purchase>(request.NewPurchaseDto);
                purchaseEntity.Created_At = DateTime.UtcNow;
                var response = await _service.AddPurchase(purchaseEntity);
                if (response > 0)
                {
                    return new()
                    {
                        Id = response,
                        Success = true,
                        Message = "Purchase has been created successfully",
                        StatusCode = StatusCodes.Status201Created
                    };
                }
                throw new Exception("Failed to create purchase");
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
