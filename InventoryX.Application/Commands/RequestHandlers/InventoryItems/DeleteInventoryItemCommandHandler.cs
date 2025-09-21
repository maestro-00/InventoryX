using InventoryX.Application.Commands.Requests.InventoryItems; 
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using System.Transactions;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Commands.RequestHandlers.InventoryItems
{
    public class DeleteInventoryItemCommandHandler(IInventoryItemService service, IRetailStockService retailStockService) : IRequestHandler<DeleteInventoryItemCommand, ApiResponse>
    {
        private readonly IInventoryItemService _service = service;
        private readonly IRetailStockService _retailStockService = retailStockService; 
        public async Task<ApiResponse> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                var response = await _service.DeleteInventoryItem(request.Id);
                if (response > 0)
                {
                    RetailStock? result = await _retailStockService.GetRetailStock("InventoryItemId", request.Id);
                    if (result is not null)
                    {
                        int deleteResponse = await _retailStockService.DeleteRetailStock(result.Id);
                        if (deleteResponse <= 0) throw new Exception("Failed to delete retail stock.");
                    }
                    transactionScope.Complete();
                    return new()
                    {
                        Success = true,
                        Message = "Inventory Item has been deleted successfully",
                        StatusCode = StatusCodes.Status200OK
                    };
                }
                throw new Exception("Failed to delete Inventory Item");
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
