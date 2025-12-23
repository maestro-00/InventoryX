using InventoryX.Application.Commands.Requests.SaleGroups;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.SaleGroups;

public class DeleteSaleGroupCommandHandler(ISaleGroupService saleGroupService) : IRequestHandler<DeleteSaleGroupCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(DeleteSaleGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var saleGroup = await saleGroupService.GetSaleGroup(request.Id);
            if (saleGroup == null) throw new CustomException("Sale not found", StatusCodes.Status404NotFound);

            var result = await saleGroupService.DeleteSaleGroup(saleGroup.Id);
            if (result <= 0) throw new Exception("Failed to delete sale.");
            return new ApiResponse
            {
                StatusCode = StatusCodes.Status202Accepted,
                Message = "Sale has been deleted successfully.",
                Success = true
            };
        }
        catch (CustomException e)
        {
            return new ApiResponse { StatusCode = e.StatusCode, Message = e.Message, Success = false };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError, Message = e.Message, Success = false
            };
        }
    }
}
