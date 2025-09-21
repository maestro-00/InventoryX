using InventoryX.Application.Commands.RequestHandlers.InventoryItems;
using InventoryX.Application.Commands.Requests.InventoryItems;
using Microsoft.AspNetCore.Http;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.InventoryItems;

public class CreateInventoryItemCommandHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IInventoryItemService> _serviceMock;
    private readonly Mock<IRetailStockService> _retailStockMock;
    private readonly CreateInventoryItemCommandHandler _sut;
    private readonly Mock<IMapper>? _mapperMock;
    private readonly int _newlyCreatedIdResponse;
    private readonly int _failedResponse;
    private readonly CreateInventoryItemCommand? _createCommand;
    private readonly CancellationToken _cancellationToken;

    public CreateInventoryItemCommandHandlerTests()
    {
        _fixture = new Fixture();
        _serviceMock = _fixture.Freeze<Mock<IInventoryItemService>>();
        _mapperMock = _fixture.Freeze<Mock<IMapper>>(); 
        _retailStockMock = new Mock<IRetailStockService>();
        _sut = new CreateInventoryItemCommandHandler(_serviceMock.Object,_retailStockMock.Object, _mapperMock.Object);
        var inventoryItem = _fixture.Create<InventoryItem>();
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(inventoryItem);
        _newlyCreatedIdResponse = 1;
        _failedResponse = -1;
        _createCommand = _fixture.Create<CreateInventoryItemCommand>();
        _cancellationToken = _fixture.Create<CancellationToken>();
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldCallAddInventoryItemMethod()
    {
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_newlyCreatedIdResponse);
        // _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
        //     .ReturnsAsync(_successResponse);
        
        await _sut.Handle(_createCommand, _cancellationToken);
        
        _serviceMock.Verify(it => it.AddInventoryItem(It.Is<InventoryItem>(i => 
            i.Created_At != null &&
            i.Created_At.Value.Date == DateTime.UtcNow.Date
            )), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenAddInventoryItemServiceSuccessful_ShouldCallAddRetailStockMethod()
    {
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_newlyCreatedIdResponse);
        
        await _sut.Handle(_createCommand, _cancellationToken);
        
        _retailStockMock.Verify(it => it.AddRetailStock(It.Is<RetailStock>(r =>
            r.InventoryItemId == _newlyCreatedIdResponse &&
            r.Quantity == _createCommand.RetailQuantity &&
            r.Created_At != null &&
            r.Created_At.Value.Date == DateTime.UtcNow.Date
            )), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenAddInventoryItemServiceFailed_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_failedResponse);
        // _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
        //     .ReturnsAsync(genericResponse); 
        
        
        var result = await _sut.Handle(_createCommand, _cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError); 
    }
    
    [Fact]
    public async Task Handle_WhenAddRetailStockServiceFailed_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_newlyCreatedIdResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(_failedResponse); 
        
        
        var result = await _sut.Handle(_createCommand, _cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError); 
    }
    
    [Fact]
    public async Task Handle_WhenBothInventoryItemAndRetailStockAddedSuccessful_ShouldReturnSuccessApiResponse()
    { 
        var inventoryItem = _fixture.Create<InventoryItem>();
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(inventoryItem);
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_newlyCreatedIdResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(_newlyCreatedIdResponse); 
        
        
        var result = await _sut.Handle(_createCommand, _cancellationToken);

        result.Should().NotBeNull();
        result.Id.Should().Be(_newlyCreatedIdResponse);
        result.Success.Should().BeTrue(); 
        result.StatusCode.Should().Be(StatusCodes.Status201Created); 
    }
    
    [Fact]
    public async Task Handle_WhenCalled_ShouldMapCorrectDto()
    { 
        // _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
        //     .ReturnsAsync(_successResponse);
        // _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
        //     .ReturnsAsync(_failedResponse); 
        
        
         await _sut.Handle(_createCommand, _cancellationToken);

        _mapperMock.Verify(x => x.Map<InventoryItem>(_createCommand.NewInventoryItemDto), Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task Handle_WhenAddInventoryItemThrowsException_ShouldReturnFailedApiResponse()
    { 
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ThrowsAsync(new Exception("Exception thrown"));
        // _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
        //     .ReturnsAsync(genericSuccessResponse); 
        
        
        var result = await _sut.Handle(_createCommand, _cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }
    
    [Fact]
    public async Task Handle_WhenAddRetailStockThrowsException_ShouldReturnFailedApiResponse()
    { 
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(_newlyCreatedIdResponse); 
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ThrowsAsync(new Exception("Exception thrown"));
        
        
        var result = await _sut.Handle(_createCommand, _cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }
    
}