    using AutoMapper;
    using InventoryX.Application.Commands.Requests.InventoryItems; 
    using InventoryX.Application.Services.IServices;
    using InventoryX.Domain.Models;
    using MediatR;
    using System.Transactions;
    using Microsoft.AspNetCore.Http;

    namespace InventoryX.Application.Commands.RequestHandlers.InventoryItems
    {
        public class CreateInventoryItemCommandHandler(IInventoryItemService service, IRetailStockService retailStockService, IMapper mapper) : IRequestHandler<CreateInventoryItemCommand, ApiResponse>
        {
            private readonly IInventoryItemService _service = service;
            private readonly IRetailStockService _retailStockService = retailStockService;
            private readonly IMapper _mapper = mapper;
            public async Task<ApiResponse> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
            {
                using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                try
                {
                    var InventoryItemEntity = _mapper.Map<InventoryItem>(request.NewInventoryItemDto);
                    InventoryItemEntity.Created_At = DateTime.UtcNow;
                    var response = await _service.AddInventoryItem(InventoryItemEntity);
                    if (response > 0)
                    {
                        RetailStock retailStock = new() { InventoryItemId = response, Quantity = request.RetailQuantity, Created_At = DateTime.UtcNow };
                        var result = await _retailStockService.AddRetailStock(retailStock);
                        if (result > 0)
                        {
                            transactionScope.Complete();
                            return new()
                            {
                                Id = response,
                                Success = true,
                                Message = "Inventory Item has been created successfully",
                                StatusCode = StatusCodes.Status201Created
                            };
                        } 
                    }
                    throw new Exception("Failed to create Inventory Item");
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
