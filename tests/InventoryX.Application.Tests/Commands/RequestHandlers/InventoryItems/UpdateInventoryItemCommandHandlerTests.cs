using InventoryX.Application.Commands.RequestHandlers.InventoryItems;
using InventoryX.Application.Commands.Requests.InventoryItems;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.InventoryItems;

public class UpdateInventoryItemCommandHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IInventoryItemService> _serviceMock;
    private readonly Mock<IRetailStockService> _retailStockMock;
    private readonly Mock<ISaleService> _salesMock;
    private readonly UpdateInventoryItemCommandHandler _sut;
    private readonly Mock<IMapper>? _mapperMock;
    private readonly UpdateInventoryItemCommand _updateCommand;
    private readonly int _successResponse;
    private readonly RetailStock? _retailStock;
    private readonly InventoryItem? _inventoryItem;
    private readonly CancellationToken _token;
    private readonly int _failedResponse;

    public UpdateInventoryItemCommandHandlerTests()
    {
        _fixture = new Fixture();
        //Mocks
        _serviceMock = _fixture.Freeze<Mock<IInventoryItemService>>();
        _mapperMock = _fixture.Freeze<Mock<IMapper>>(); 
        _retailStockMock = new Mock<IRetailStockService>();
        _salesMock = new Mock<ISaleService>();
        _inventoryItem = _fixture.Create<InventoryItem>();
        _updateCommand = _fixture.Create<UpdateInventoryItemCommand>();
        _successResponse = 1;
        _retailStock = _fixture.Create<RetailStock>();
        _token = _fixture.Create<CancellationToken>();
        _failedResponse = -1;
        
        _sut = new UpdateInventoryItemCommandHandler(_serviceMock.Object,_retailStockMock.Object, _mapperMock.Object,_salesMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldMapCorrectInventoryItemDto()
    {
        await _sut.Handle(_updateCommand, _token);
        
        _mapperMock.Verify(r => r.Map<InventoryItem>(It.Is<InventoryItemCommandDto>(x => x == _updateCommand.InventoryItemDto)));
    }
    
    [Fact]
    public async Task Handle_WhenCalled_ShouldUpdateInventoryItem()
    {
        _updateCommand.RecordLoss = false;
        
        await _sut.Handle(_updateCommand, _token);

        _serviceMock.Verify(x => x.UpdateInventoryItem(It.Is<InventoryItem>(r =>
            r.Id == _updateCommand.Id &&
            r.Updated_At.HasValue
            )), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenInventoryItemUpdateFails_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(x => x.UpdateInventoryItem(_inventoryItem))
            .ReturnsAsync(_failedResponse);

        var result = await _sut.Handle(_updateCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenInventoryItemUpdateThrowsException_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(x => x.UpdateInventoryItem(_inventoryItem))
            .Throws(new Exception("Exception thrown"));

        var result = await _sut.Handle(_updateCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenRecordLossTrue_ShouldGetOldInventoryItem()
    {
        var command = _fixture.Build<UpdateInventoryItemCommand>()
            .With(r => r.RecordLoss, true).Create();
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
    
        await _sut.Handle(command, _token);
        
        _serviceMock.Verify(r => r.GetInventoryItem(command.Id));
    }
    [Fact]
    public async Task Handle_WhenGetInventoryItemReturnsNull_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _serviceMock.Setup(x => x.GetInventoryItem(_updateCommand.Id))
            .ReturnsAsync(() => null); 
    
        var result = await _sut.Handle(_updateCommand, _token);
        
        _salesMock.Verify(v => v.AddSale(It.IsAny<Sale>()), Times.Never);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    [Fact]
    public async Task Handle_WhenGetInventoryItemThrowsException_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _serviceMock.Setup(x => x.GetInventoryItem(_updateCommand.Id))
            .Throws(new Exception("Exception thrown")); 
    
        var result = await _sut.Handle(_updateCommand, _token);
        
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenOldInventoryItemAmountExceedsCurrentInventoryItemAmount_ShouldAddSale()
    {
        _inventoryItem.TotalAmount = 1;
        var oldInventoryItem = _fixture.Build<InventoryItem>()
            .With(r => r.TotalAmount, 2).Create();
        var commandDto = _fixture.Build<InventoryItemCommandDto>()
            .With(r => r.TotalAmount, 1).Create();
        var command = _fixture.Build<UpdateInventoryItemCommand>()
            .With(r => r.RecordLoss, true)
            .With(r => r.InventoryItemDto, commandDto).Create();
        _serviceMock.Setup(x => x.GetInventoryItem(command.Id))
            .ReturnsAsync(oldInventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.Is<InventoryItemCommandDto>(x => x == commandDto)))
            .Returns(_inventoryItem);
        
        await _sut.Handle(command, _token);
        
        _salesMock.Verify(x => x.AddSale(It.Is<Sale>(r => 
            r.InventoryItemId == command.Id &&
            r.Price == 0 &&
            r.Quantity == oldInventoryItem.TotalAmount - _inventoryItem.TotalAmount &&
            r.Created_At.HasValue &&
            r.Created_At.Value.Date == DateTime.UtcNow.Date
        )), Times.Once);
    }
    [Fact]
    public async Task Handle_WhenOldInventoryItemAmountNotGreaterThanCurrentInventoryItemAmount_ShouldNotAddSale()
    {
        _inventoryItem.TotalAmount = 1;
        var oldInventoryItem = _fixture.Build<InventoryItem>()
            .With(r => r.TotalAmount, 1).Create();
        var commandDto = _fixture.Build<InventoryItemCommandDto>()
            .With(r => r.TotalAmount, 1).Create();
        var command = _fixture.Build<UpdateInventoryItemCommand>()
            .With(r => r.RecordLoss, true)
            .With(r => r.InventoryItemDto, commandDto).Create();
        _serviceMock.Setup(x => x.GetInventoryItem(command.Id))
            .ReturnsAsync(oldInventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.Is<InventoryItemCommandDto>(x => x == commandDto)))
            .Returns(_inventoryItem);
        
        await _sut.Handle(command, _token);
        
        _salesMock.Verify(x => x.AddSale(It.IsAny<Sale>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_WhenAddSaleMethodFails_ShouldReturnFailedApiResponse()
    {
        _inventoryItem.TotalAmount = 1;
        var oldInventoryItem = _fixture.Build<InventoryItem>()
            .With(r => r.TotalAmount, 2).Create();
        var commandDto = _fixture.Build<InventoryItemCommandDto>()
            .With(r => r.TotalAmount, 1).Create();
        var command = _fixture.Build<UpdateInventoryItemCommand>()
            .With(r => r.RecordLoss, true)
            .With(r => r.InventoryItemDto, commandDto).Create();
        _serviceMock.Setup(x => x.GetInventoryItem(command.Id))
            .ReturnsAsync(oldInventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _salesMock.Setup(x => x.AddSale(It.Is<Sale>(s => s.Quantity == 1)))
            .ReturnsAsync(_failedResponse);
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.Is<InventoryItemCommandDto>(x => x == commandDto)))
            .Returns(_inventoryItem);

        var result = await _sut.Handle(command, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenAddSaleMethodThrowsException_ShouldReturnFailedApiResponse()
    {
        _inventoryItem.TotalAmount = 1;
        _updateCommand.InventoryItemDto.TotalAmount = 1;
        _updateCommand.RecordLoss = true;
        var oldInventoryItem = _fixture.Build<InventoryItem>()
            .With(r => r.TotalAmount, 2).Create();
        _serviceMock.Setup(x => x.GetInventoryItem(_updateCommand.Id))
            .ReturnsAsync(oldInventoryItem);
        _salesMock.Setup(x => x.AddSale(It.Is<Sale>(s => s.Quantity == 1)))
            .Throws(new Exception("Exception thrown"));
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse); 
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.Is<InventoryItemCommandDto>(x => x == _updateCommand.InventoryItemDto)))
            .Returns(_inventoryItem);
        
        var result = await _sut.Handle(_updateCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenInventoryItemUpdateSuccessful_ShouldGetExistingRetailStock()
    {
        _updateCommand.RecordLoss = false;
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        
        await _sut.Handle(_updateCommand, _token);
        
        _retailStockMock.Verify(r => r.GetRetailStock(It.IsAny<string>(), _updateCommand.Id));
    }
    
    [Fact]
    public async Task Handle_WhenGetRetailStockThrowsException_ShouldReturnFailedApiResponse()
    {
        _updateCommand.RecordLoss = false;
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), _updateCommand.Id))
            .Throws(new Exception("Exception Thrown"));
        
        var result = await _sut.Handle(_updateCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenGetRetailStockReturnsNull_ShouldReturnFailedApiResponse()
    {
        _updateCommand.RecordLoss = false;
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), _updateCommand.Id))
            .ReturnsAsync(() => null);
        
        await _sut.Handle(_updateCommand, _token);

        _retailStockMock.Verify(r => r.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_WhenRetailStockQuantityGreaterThanInventoryItemAmount_ShouldUpdateRetailStock()
    {
        _inventoryItem.TotalAmount = 1;
        _retailStock.Quantity = 2;
        _updateCommand.RecordLoss = false;
        _mapperMock.Setup(x => x.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(_inventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), _updateCommand.Id))
            .ReturnsAsync(_retailStock);
        
       await _sut.Handle(_updateCommand, _token);

        _retailStockMock.Verify(x => x.UpdateRetailStock(It.Is<RetailStock>(r => 
            r.Quantity == _inventoryItem.TotalAmount &&
            r.Updated_At.HasValue &&
            r.Updated_At.Value.Date == DateTime.UtcNow.Date
        )), Times.Once);
    }
    [Fact]
    public async Task Handle_WhenRetailStockQuantityNotGreaterThanInventoryItemAmount_ShouldNotUpdateRetailStock()
    {
        _inventoryItem.TotalAmount = 1;
        _retailStock.Quantity = 1;
        _updateCommand.RecordLoss = false;
        _mapperMock.Setup(x => x.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(_inventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), _updateCommand.Id))
            .ReturnsAsync(_retailStock);
        
       await _sut.Handle(_updateCommand, _token);

        _retailStockMock.Verify(x => x.UpdateRetailStock(It.IsAny<RetailStock>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_WhenUpdatingRetailStockFails_ShouldReturnFailedApiResponse()
    {
        _inventoryItem.TotalAmount = 1;
        _retailStock.Quantity = 2;
        _updateCommand.RecordLoss = false;
        _mapperMock.Setup(x => x.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(_inventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), _updateCommand.Id))
            .ReturnsAsync(_retailStock);
        _retailStockMock.Setup(x => x.UpdateRetailStock(_retailStock))
            .ReturnsAsync(_failedResponse);
        
        var result = await _sut.Handle(_updateCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenUpdatingRetailStockThrowsException_ShouldReturnFailedApiResponse()
    {
        _inventoryItem.TotalAmount = 1;
        _retailStock.Quantity = 2;
        _updateCommand.RecordLoss = false;
        _mapperMock.Setup(x => x.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(_inventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), _updateCommand.Id))
            .ReturnsAsync(_retailStock);
        _retailStockMock.Setup(x => x.UpdateRetailStock(_retailStock))
            .ThrowsAsync(new Exception("Exception Thrown"));
        
        
        var result = await _sut.Handle(_updateCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenAllRequiredOperationsSuccessful_ShouldReturnSuccessApiResponse()
    {
        _inventoryItem.TotalAmount = 1;
        _retailStock.Quantity = 2; 
        var oldInventoryItem = _fixture.Build<InventoryItem>()
            .With(r => r.TotalAmount, 2).Create();
        var commandDto = _fixture.Build<InventoryItemCommandDto>()
            .With(r => r.TotalAmount, 1).Create();
        var command = _fixture.Build<UpdateInventoryItemCommand>()
            .With(r => r.RecordLoss, true)
            .With(r => r.InventoryItemDto, commandDto).Create();
        
        _mapperMock.Setup(x => x.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(_inventoryItem);
        _salesMock.Setup(x => x.AddSale(It.Is<Sale>(s => s.Quantity == 1)))
            .ReturnsAsync(_successResponse);
        _serviceMock.Setup(x => x.GetInventoryItem(command.Id))
            .ReturnsAsync(oldInventoryItem);
        _serviceMock.Setup(x => x.UpdateInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(x => x.GetRetailStock(It.IsAny<string>(), command.Id))
            .ReturnsAsync(_retailStock);
        _retailStockMock.Setup(x => x.UpdateRetailStock(_retailStock))
            .ReturnsAsync(_successResponse);
        
        var result = await _sut.Handle(command, _token);

        _retailStockMock.Verify(x => x.UpdateRetailStock(It.Is<RetailStock>(r => 
            r.Quantity == _inventoryItem.TotalAmount &&
            r.Updated_At.HasValue &&
            r.Updated_At.Value.Date == DateTime.UtcNow.Date
        )), Times.Once);
        _salesMock.Verify(x => x.AddSale(It.Is<Sale>(r => 
            r.InventoryItemId == command.Id &&
            r.Price == 0 &&
            r.Quantity == oldInventoryItem.TotalAmount - _inventoryItem.TotalAmount &&
            r.Created_At.HasValue &&
            r.Created_At.Value.Date == DateTime.UtcNow.Date
        )), Times.Once);
        _serviceMock.Verify(x => x.UpdateInventoryItem(It.Is<InventoryItem>(r =>
            r.Id == command.Id &&
            r.Updated_At.HasValue
        )), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        result.Success.Should().BeTrue();
        result.Id.Should().Be(command.Id);
    }
    
}