using AutoMapper;
using InventoryX.Application.Commands.Requests.InventoryItems; 
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using System.Transactions;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.InventoryItems
{
    public class UpdateInventoryItemCommandHandler(IInventoryItemService service, IRetailStockService retailStockService, IMapper mapper, ISaleService saleService) : IRequestHandler<UpdateInventoryItemCommand, ApiResponse>
    {
        private readonly IInventoryItemService _service = service;
        private readonly IRetailStockService _retailStockService = retailStockService;
        private readonly IMapper _mapper = mapper;
        private readonly ISaleService _saleService = saleService; 

        public async Task<ApiResponse> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try
            { 
                var InventoryItemEntity = _mapper.Map<InventoryItem>(request.InventoryItemDto);
                InventoryItemEntity.Id = request.Id;
                InventoryItemEntity.Updated_At = DateTime.UtcNow;
                InventoryItem? OldInventoryItem;
                int response = await _service.UpdateInventoryItem(InventoryItemEntity);
                if (response <= 0) throw new Exception("Failed to update Inventory Item");
                
                //Create sale record only if loss should be recorded
                if (request.RecordLoss)
                {
                    OldInventoryItem = await _service.GetInventoryItem(request.Id);
                    var quantityDiff = OldInventoryItem.TotalAmount - InventoryItemEntity.TotalAmount;
                    if(quantityDiff > 0)
                    {
                        var saleResponse = await _saleService.AddSale(new() { InventoryItem = null, Seller = null, UserId = null, InventoryItemId = InventoryItemEntity.Id, Price = 0, Quantity = quantityDiff, Created_At = DateTime.UtcNow });
                        if (saleResponse <= 0) throw new Exception("Failed to update Inventory Item amount as loss");
                    }
                }                     
                var retailStock = await _retailStockService.GetRetailStock("InventoryItemId", request.Id);

                if (retailStock is not null)
                {
                    if(retailStock.Quantity > InventoryItemEntity.TotalAmount)
                    {
                        retailStock.Quantity = InventoryItemEntity.TotalAmount;
                        retailStock.Updated_At = DateTime.UtcNow;
                        int result = await _retailStockService.UpdateRetailStock(retailStock);

                        if (result <= 0) throw new Exception("Failed to update Inventory Item. Failed to reset Retail Stock price.");
                    }
                }
                transactionScope.Complete();
                return new()
                {
                    Id = InventoryItemEntity.Id,
                    Success = true,
                    Message = "Inventory Item has been updated successfully",
                    StatusCode = StatusCodes.Status202Accepted
                };
                throw new Exception("Failed to update Inventory Item");
            }
            catch (Exception ex)
            {
                transactionScope.Dispose();
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
