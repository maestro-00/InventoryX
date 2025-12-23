using AutoMapper;
using InventoryX.Application.DTOs.SaleGroups;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Application.Queries.Requests.SaleGroups;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.SaleGroups;

public class GetAllSaleGroupsRequestHandler(ISaleGroupService saleGroupService, ISaleService saleService, IMapper mapper) : IRequestHandler<GetAllSaleGroupsRequest, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetAllSaleGroupsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<SaleGroup> saleGroups = await saleGroupService.GetAllSaleGroups();
            IEnumerable<SaleGroupDto> saleGroupDtos = mapper.Map<IEnumerable<SaleGroupDto>>(saleGroups);
            foreach (SaleGroupDto saleGroupDto in saleGroupDtos)
            {
                List<Sale> sales = await saleService.GetSalesByGroupId(saleGroupDto.Id);
                IEnumerable<GetSaleDto>? salesDtos = mapper.Map<IEnumerable<GetSaleDto>>(sales);
                saleGroupDto.Sales = salesDtos;
            }

            return new ApiResponse
            {
                Message = "Retrieved Sale Groups successfully.",
                StatusCode = StatusCodes.Status200OK,
                Body = saleGroupDtos,
                Success = true
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Success = false,
                Message = e.Message,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
