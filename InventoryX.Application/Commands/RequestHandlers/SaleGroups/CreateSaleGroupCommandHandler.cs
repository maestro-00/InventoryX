using System.Transactions;
using AutoMapper;
using InventoryX.Application.Commands.Requests.SaleGroups;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.SaleGroups;

public class CreateSaleGroupCommandHandler(ISaleGroupService saleGroupService, ISaleService saleService, IInventoryItemService inventoryItemService, IRetailStockService retailStockService, IMapper mapper) : IRequestHandler<CreateSaleGroupCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreateSaleGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var saleGroupEntity = mapper.Map<SaleGroup>(request.SaleGroupCommandDto);
            saleGroupEntity.Created_At = DateTime.UtcNow;
            var saleGroupResult =  await saleGroupService.AddSaleGroup(saleGroupEntity);
            if (saleGroupResult <= 0) throw new InvalidDataException("Failed to make sale.");
            //TODO: Use bulk insert to persist sales data
            foreach (var sale in request.SaleGroupCommandDto.Sales)
            {
                var saleEntity = mapper.Map<Sale>(sale);
                saleEntity.Created_At = DateTime.UtcNow;
                saleEntity.SaleGroupId = saleGroupEntity.Id;
                var saleResult = await saleService.AddSale(saleEntity);
                if (saleResult <= 0) throw new InvalidDataException("Failed to make sale.");
                var retailStock = await retailStockService.GetRetailStock("InventoryItemId", sale.InventoryItemId);
                if (retailStock == null) throw new CustomException("Failed to make sale.", StatusCodes.Status500InternalServerError);
                retailStock.Quantity -= sale.Quantity;
                var updateRetailStock = await retailStockService.UpdateRetailStock(retailStock);
                if(updateRetailStock <= 0) throw new Exception("Failed to create sale");

                saleEntity.InventoryItem.TotalAmount -= sale.Quantity;

                var updateInventoryResult = await inventoryItemService.UpdateInventoryItem(saleEntity.InventoryItem);
                if(updateInventoryResult <= 0) throw new Exception("Failed to create sale");

            }

            transactionScope.Complete();
            return new ApiResponse
            {
                StatusCode = StatusCodes.Status201Created,
                Message = "Sale has been made successfully.",
                Success = true,
                Id = saleGroupEntity.Id
            };
        }
        catch (Exception e)
        {
            return new ApiResponse { StatusCode = StatusCodes.Status500InternalServerError, Message = e.Message, Success = false };
        }

    }
}
