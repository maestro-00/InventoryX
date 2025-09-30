using AutoMapper;
using InventoryX.Application.Commands.Requests.Purchases;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.Purchases
{
    public class DeletePurchaseCommandHandler(IPurchaseService service) : IRequestHandler<DeletePurchaseCommand, ApiResponse>
    {
        private readonly IPurchaseService _service = service; 
        public async Task<ApiResponse> Handle(DeletePurchaseCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Id < 1)
                {
                    return new ApiResponse
                    {
                        Message = "Invalid Purchase Id Passed",
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false
                    };
                }
                var response = await _service.DeletePurchase(request.Id);
                if (response > 0)
                {
                    return new()
                    {
                        Success = true,
                        Message = "Purchase has been deleted successfully",
                        StatusCode = StatusCodes.Status202Accepted,
                        Id = request.Id
                    };
                }
                throw new Exception("Failed to delete Purchase");
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
