using AutoMapper; 
using InventoryX.Application.Commands.Requests.Purchases; 
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.Purchases
{
    public class UpdatePurchaseCommandHandler(IPurchaseService service, IMapper mapper) : IRequestHandler<UpdatePurchaseCommand, ApiResponse>
    {
        private readonly IPurchaseService _service = service;
        private readonly IMapper _mapper = mapper;

        public async Task<ApiResponse> Handle(UpdatePurchaseCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id < 1)
                {
                    return new ApiResponse
                    {
                        Message = "Purchase Id is invalid.",
                        Success = false,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                var purchaseEntity = _mapper.Map<Purchase>(request.PurchaseDto);
                purchaseEntity.Id = request.Id;
                purchaseEntity.Updated_At = DateTime.UtcNow;
                var response = await _service.UpdatePurchase(purchaseEntity);
                if (response > 0)
                {
                    return new()
                    {
                        Id = purchaseEntity.Id,
                        Success = true,
                        Message = "Purchase has been updated successfully",
                        StatusCode = StatusCodes.Status202Accepted
                    };
                }
                throw new Exception("Failed to update Purchase");
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
