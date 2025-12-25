using InventoryX.Application.Queries.Requests.SaleGroups;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.SaleGroups;

public class GetSaleStatsRequestHandler(ISaleService saleService) : IRequestHandler<GetSaleStatsRequest, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetSaleStatsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var saleStats = await saleService.GetSaleStats(cancellationToken);
            return new ApiResponse()
            {
                Success = true,
                Body = saleStats,
                Message = "Retrieved sales stats successfully",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception e)
        {
            return new ApiResponse()
            {
                Success = false,
                Message = "Something went wrong. Please try again later.",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
