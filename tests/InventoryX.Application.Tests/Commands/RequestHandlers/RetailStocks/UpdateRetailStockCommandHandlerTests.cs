using InventoryX.Application.Commands.RequestHandlers.RetailStocks;
using InventoryX.Application.Commands.Requests.RetailStock; 

namespace InventoryX.Application.Tests.Commands.RequestHandlers.RetailStocks;

public class UpdateRetailStockCommandHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemThrowsException_ShouldReturnFailedResponse(
        RetailStock retailStock,
        [Frozen] Mock<IInventoryItemService> serviceMock,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<IRetailStockService> retailStockServiceMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock);
        serviceMock.Setup(s => s.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Throws(new Exception("Service exception"));
            
        var result = await handler.Handle(command, cancellationToken);
        
        serviceMock.Verify(s => s.GetInventoryItem(retailStock.InventoryItemId), Times.Once);
        retailStockServiceMock.Verify(s => s.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Service exception");
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemReturnsNull_ShouldReturnFailedResponse(
        RetailStock retailStock,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<IRetailStockService> retailStockServiceMock,
        [Frozen] Mock<IInventoryItemService> serviceMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock);
        serviceMock.Setup(s => s.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Returns((InventoryItem?)null);
            
        var result = await handler.Handle(command, cancellationToken);
        
        retailStockServiceMock.Verify(s => s.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
        serviceMock.Verify(s => s.GetInventoryItem(retailStock.InventoryItemId), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Message.Should().Contain("Inventory item not found");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetRetailStockReturnsNull_ShouldReturnFailedResponse(
        [Frozen] Mock<IRetailStockService> stockMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        stockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(),command.RetailStock.InventoryItemId).Result)
            .Returns((RetailStock?)null);
            
        var result = await handler.Handle(command, cancellationToken);
        
        stockMock.Verify(s => s.GetRetailStock("InventoryItemId", command.RetailStock.InventoryItemId), Times.Once);
        stockMock.Verify(s => s.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Message.Should().Contain("Retail stock not found");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetRetailStockThrowsException_ShouldReturnFailedResponse(
        [Frozen] Mock<IRetailStockService> stockMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        stockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(),command.RetailStock.InventoryItemId).Result)
            .Throws(new Exception("Service exception"));
            
        var result = await handler.Handle(command, cancellationToken);
        
        stockMock.Verify(s => s.GetRetailStock("InventoryItemId", command.RetailStock.InventoryItemId), Times.Once);
        stockMock.Verify(s => s.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Service exception");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenUpdateRetailStockFails_ShouldReturnFailedResponse(
        RetailStock retailStock,
        InventoryItem inventoryItem,
        [Frozen] Mock<IInventoryItemService> inventoryItemMock,
        [Frozen] Mock<IRetailStockService> serviceMock,
        [Frozen] Mock<IMapper> mapperMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var failedResponse = 0;
        inventoryItem.TotalAmount = 1;
        retailStock.Quantity = 1;
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock);
        inventoryItemMock.Setup(i => i.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Returns(inventoryItem);
        serviceMock.Setup(s => s.UpdateRetailStock(It.IsAny<RetailStock>()).Result)
            .Returns(failedResponse);
            
        var result = await handler.Handle(command, cancellationToken);
        
        serviceMock.Verify(s => s.UpdateRetailStock(retailStock), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError); 
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenUpdateRetailStockThrowsException_ShouldReturnFailedResponse(
        RetailStock retailStock,
        InventoryItem inventoryItem,
        [Frozen] Mock<IInventoryItemService> inventoryItemMock,
        [Frozen] Mock<IRetailStockService> serviceMock,
        [Frozen] Mock<IMapper> mapperMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    { 
        inventoryItem.TotalAmount = 1;
        retailStock.Quantity = 1;
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock);
        inventoryItemMock.Setup(i => i.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Returns(inventoryItem);
        serviceMock.Setup(s => s.UpdateRetailStock(It.IsAny<RetailStock>()).Result)
            .Throws(new Exception("Service exception"));
            
        var result = await handler.Handle(command, cancellationToken);
        
        serviceMock.Verify(s => s.UpdateRetailStock(retailStock), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Service exception");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenCommandDtoIsNull_ShouldReturnFailedResponse( 
        [Frozen] Mock<IRetailStockService> serviceMock, 
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        command.RetailStock = null;  
            
        var result = await handler.Handle(command, cancellationToken);
        
        serviceMock.Verify(s => s.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest); 
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ShouldReturnFailedResponse( 
        [Frozen] Mock<IRetailStockService> serviceMock,
        [Frozen] Mock<IMapper> mapperMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Throws(new Exception("Mapper exception")); 
            
        var result = await handler.Handle(command, cancellationToken);
        
        serviceMock.Verify(s => s.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Mapper exception");
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenRetailQuantityGreaterThanInventoryAmount_ShouldReturnFailedResponse(
        RetailStock retailStock,
        InventoryItem inventoryItem,
        [Frozen] Mock<IInventoryItemService> serviceMock,
        [Frozen] Mock<IRetailStockService> stockMock,
        [Frozen] Mock<IMapper> mapperMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        inventoryItem.TotalAmount = 1;
        retailStock.Quantity = 2;
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock); 
        serviceMock.Setup(s => s.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Returns(inventoryItem);
            
        var result = await handler.Handle(command, cancellationToken);
        
        stockMock.Verify(s => s.UpdateRetailStock(retailStock), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest); 
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenRetailQuantityNotGreaterThanInventoryAmount_ShouldReturnSuccessResponse(
        RetailStock retailStock,
        InventoryItem inventoryItem,
        [Frozen] Mock<IInventoryItemService> serviceMock,
        [Frozen] Mock<IRetailStockService> stockMock,
        [Frozen] Mock<IMapper> mapperMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var successResponse = 1;
        inventoryItem.TotalAmount = 1;
        retailStock.Quantity = 1;
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock); 
        serviceMock.Setup(s => s.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Returns(inventoryItem);
        stockMock.Setup(s => s.UpdateRetailStock(retailStock).Result)
            .Returns(successResponse);
            
        var result = await handler.Handle(command, cancellationToken);
        
        stockMock.Verify(s => s.UpdateRetailStock(retailStock), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted); 
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenUpdateRetailQuantitySucceeds_ShouldReturnSuccessResponse(
        RetailStock retailStock,
        InventoryItem inventoryItem,
        [Frozen] Mock<IInventoryItemService> serviceMock,
        [Frozen] Mock<IRetailStockService> stockMock,
        [Frozen] Mock<IMapper> mapperMock,
        UpdateRetailStockCommand command,
        UpdateRetailStockCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var successResponse = 1;
        inventoryItem.TotalAmount = 1;
        retailStock.Quantity = 1;
        mapperMock.Setup(m => m.Map<RetailStock>(command.RetailStock))
            .Returns(retailStock);
        serviceMock.Setup(s => s.GetInventoryItem(retailStock.InventoryItemId).Result)
            .Returns(inventoryItem);
        stockMock.Setup(s => s.UpdateRetailStock(retailStock).Result)
            .Returns(successResponse);
            
        var result = await handler.Handle(command, cancellationToken);
        
        stockMock.Verify(s => s.UpdateRetailStock(It.Is<RetailStock>(r =>
            r.Id == retailStock.Id &&
            r.Updated_At.HasValue &&
            r.Updated_At.Value.Date == DateTime.UtcNow.Date
            )), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted); 
    }
    
    
}