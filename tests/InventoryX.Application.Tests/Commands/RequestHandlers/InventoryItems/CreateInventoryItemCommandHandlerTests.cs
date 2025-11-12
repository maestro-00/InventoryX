using InventoryX.Application.Commands.RequestHandlers.InventoryItems;
using InventoryX.Application.Commands.Requests.InventoryItems;
using InventoryX.Application.DTOs.InventoryItems;

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
        _sut = new CreateInventoryItemCommandHandler(_serviceMock.Object, _retailStockMock.Object, _mapperMock.Object);
        var inventoryItem = _fixture.Create<InventoryItem>();
        _mapperMock.Setup(m => m.Map<InventoryItem>(It.IsAny<InventoryItemCommandDto>()))
            .Returns(inventoryItem);
        _newlyCreatedIdResponse = 1;
        _failedResponse = -1;
        _createCommand = _fixture.Create<CreateInventoryItemCommand>();
        //Ensuring Total Amount is lesser than retail quantity
        _createCommand.NewInventoryItemDto.TotalAmount = 1;
        _createCommand.RetailQuantity = 1;
        _cancellationToken = _fixture.Create<CancellationToken>();
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldCallAddInventoryItemMethod()
    {
        //Ensuring Total Amount is lesser than retail quantity
        _createCommand.NewInventoryItemDto.TotalAmount = 1;
        _createCommand.RetailQuantity = 1;
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
        //Ensuring Total Amount is lesser than retail quantity
        _createCommand.NewInventoryItemDto.TotalAmount = 1;
        _createCommand.RetailQuantity = 1;
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
        //Ensuring Total Amount is lesser than retail quantity
        _createCommand.NewInventoryItemDto.TotalAmount = 1;
        _createCommand.RetailQuantity = 1;
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
        //Ensuring Total Amount is lesser than retail quantity
        _createCommand.NewInventoryItemDto.TotalAmount = 1;
        _createCommand.RetailQuantity = 1;
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
        //Ensuring Total Amount is lesser than retail quantity
        _createCommand.NewInventoryItemDto.TotalAmount = 1;
        _createCommand.RetailQuantity = 1;
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

    [Theory, AutoDomainData]
    public async Task Handle_WhenRetailQuantityGreaterThanTotalAmount_ShouldReturnFailedApiResponse(
        CreateInventoryItemCommand command,
        CancellationToken token,
        CreateInventoryItemCommandHandler sut)
    {
        command.NewInventoryItemDto.TotalAmount = 1;
        command.RetailQuantity = 2;


        var result = await sut.Handle(command, token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
    [Theory, AutoDomainData]
    public async Task Handle_WhenRetailQuantityNotGreaterThanTotalAmount_ShouldReturnFailedApiResponse(
        CreateInventoryItemCommand command,
        CancellationToken token,
        [Frozen] Mock<IInventoryItemService> inventoryItemServiceMock,
        [Frozen] Mock<IRetailStockService> retailStockServiceMock,
        CreateInventoryItemCommandHandler sut)
    {
        const int successResponse = 1;
        command.NewInventoryItemDto.TotalAmount = 1;
        command.RetailQuantity = 1;
        RetailStock? retailStock = null;
        inventoryItemServiceMock.Setup(x => x.AddInventoryItem(It.IsAny<InventoryItem>()).Result)
            .Returns(successResponse);
        retailStockServiceMock.Setup(rss => rss.AddRetailStock(It.IsAny<RetailStock>()).Result)
            .Callback<RetailStock>(rS => retailStock = rS)
            .Returns(successResponse);

        var result = await sut.Handle(command, token);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().NotBe(StatusCodes.Status400BadRequest);
        inventoryItemServiceMock.Verify(iis => iis.AddInventoryItem(It.IsAny<InventoryItem>()), Times.Once);
        retailStockServiceMock.Verify(rss => rss.AddRetailStock(It.IsAny<RetailStock>()), Times.Once);
        retailStock.Quantity.Should().Be(command.RetailQuantity);
    }

}
