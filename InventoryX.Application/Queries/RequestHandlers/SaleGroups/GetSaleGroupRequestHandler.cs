using AutoMapper;
using InventoryX.Application.DTOs.SaleGroups;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.SaleGroups;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Queries.RequestHandlers.SaleGroups;

public class GetSaleGroupRequestHandler(ISaleGroupService saleGroupService, ISaleService saleService, IMapper mapper) : IRequestHandler<GetSaleGroupRequest, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetSaleGroupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            SaleGroup? saleGroup = await saleGroupService.GetSaleGroup(request.Id);
            if (saleGroup == null)
            {
                throw new CustomException("Sale Group not found", StatusCodes.Status404NotFound);
            }

            SaleGroupDto? saleGroupDto = mapper.Map<SaleGroupDto>(saleGroup);
            List<Sale> sales = await saleService.GetSalesByGroupId(saleGroupDto.Id);
            List<GetSaleDto>? saleDtos = mapper.Map<List<GetSaleDto>>(sales);
            saleGroupDto.Sales = saleDtos;
            return new ApiResponse
            {
                Success = true, StatusCode = StatusCodes.Status200OK, Message = "Sale Group retrieved successfully.", Body = saleGroupDto
            };
        }
        catch (CustomException e)
        {
            return new ApiResponse
            {
                Message = e.Message,
                StatusCode = e.StatusCode,
                Success = false
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Message = e.Message,
                StatusCode = StatusCodes.Status500InternalServerError,
                Success = false
            };
        }

    }
}
