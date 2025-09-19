using InventoryX.Application.Commands.RequestHandlers.InventoryItems;
using InventoryX.Application.Commands.Requests.InventoryItems;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.InventoryItems;

public class CreateInventoryItemCommandHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IInventoryItemService> _serviceMock;
    private readonly Mock<IRetailStockService> _retailStockMock;
    private readonly CreateInventoryItemCommandHandler _sut;
    private readonly Mock<IMapper>? _mapperMock;

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
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldCallAddInventoryItemMethod()
    {
        var genericResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(genericResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericResponse);
        
        await _sut.Handle(command, cancellationToken);
        
        _serviceMock.Verify(it => it.AddInventoryItem(It.IsAny<InventoryItem>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenCalled_ShouldUpdateCreatedAtOfBothInventoryItemAndRetailStock()
    {
        var genericResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(genericResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericResponse);
        
        await _sut.Handle(command, cancellationToken);
        
        _serviceMock.Verify(it => it.AddInventoryItem(It.Is<InventoryItem>( r => r.Created_At != null)), Times.Once);
        _retailStockMock.Verify(it => it.AddRetailStock(It.Is<RetailStock>( r => r.Created_At != null)), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenAddInventoryItemServiceSuccessful_ShouldCallAddRetailStockMethod()
    {
        var successfulInventoryItemResponse = 1;
        var genericResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>(); 
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(successfulInventoryItemResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericResponse); 
        
        
        await _sut.Handle(command, cancellationToken);
        
        _retailStockMock.Verify(it => it.AddRetailStock(It.IsAny<RetailStock>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenAddInventoryItemServiceFailed_ShouldReturnFailedApiResponse()
    {
        var failedInventoryItemResponse = -1;
        var genericResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(failedInventoryItemResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericResponse); 
        
        
        var result = await _sut.Handle(command, cancellationToken);

        result.Success.Should().Be(false);
        result.StatusCode.Should().Be(500); 
    }
    
    [Fact]
    public async Task Handle_WhenAddRetailStockServiceFailed_ShouldReturnFailedApiResponse()
    {
        var failedRetailStockResponse = -1;
        var genericResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(genericResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(failedRetailStockResponse); 
        
        
        var result = await _sut.Handle(command, cancellationToken);

        result.Success.Should().Be(false);
        result.StatusCode.Should().Be(500); 
    }
    
    [Fact]
    public async Task Handle_WhenBothAddInventoryItemServiceAndAddRetailStockServiceSuccessful_ShouldReturnSuccessApiResponse()
    { 
        var genericSuccessResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(genericSuccessResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericSuccessResponse); 
        
        
        var result = await _sut.Handle(command, cancellationToken);

        result.Success.Should().Be(true); result.StatusCode.Should().Be(201); 
    }
    
    [Fact]
    public async Task Handle_WhenCalled_ShouldMapCorrectDto()
    { 
        var genericSuccessResponse = 1;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(genericSuccessResponse);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericSuccessResponse); 
        
        
        var result = await _sut.Handle(command, cancellationToken);

        _mapperMock.Verify(x => x.Map<InventoryItem>(command.NewInventoryItemDto), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenCalled_ShouldReturnNewInventoryItemIdInApiResponse()
    { 
        var genericSuccessResponse = 1;
        var expectedNewId = 2;
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(expectedNewId);
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericSuccessResponse); 
        
        
        var result = await _sut.Handle(command, cancellationToken);

        result.Id.Should().Be(expectedNewId);
    }
    
    [Fact]
    public async Task Handle_WhenAddInventoryItemThrowsException_ShouldReturnFailedApiResponse()
    { 
        var genericSuccessResponse = 1; 
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ThrowsAsync(new Exception("Exception thrown"));
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ReturnsAsync(genericSuccessResponse); 
        
        
        var result = await _sut.Handle(command, cancellationToken);

        result.Success.Should().Be(false);
        result.StatusCode.Should().Be(500);
        result.Message.Should().Contain("Exception");
    }
    
    [Fact]
    public async Task Handle_WhenAddRetailStockThrowsException_ShouldReturnFailedApiResponse()
    { 
        var genericSuccessResponse = 1; 
        var command = _fixture.Create<CreateInventoryItemCommand>();
        var cancellationToken = _fixture.Create<CancellationToken>();
        _serviceMock.Setup(m => m.AddInventoryItem(It.IsAny<InventoryItem>()))
            .ReturnsAsync(genericSuccessResponse); 
        _retailStockMock.Setup(m => m.AddRetailStock(It.IsAny<RetailStock>()))
            .ThrowsAsync(new Exception("Exception thrown"));
        
        
        var result = await _sut.Handle(command, cancellationToken);

        result.Success.Should().Be(false);
        result.StatusCode.Should().Be(500);
        result.Message.Should().Contain("Exception");
    }
    
}