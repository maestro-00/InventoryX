using InventoryX.Application.Commands.RequestHandlers.InventoryItems;
using InventoryX.Application.Commands.Requests.InventoryItems;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.InventoryItems;

public class DeleteInventoryItemCommandHandlerTests
{
    private readonly Mock<IInventoryItemService> _serviceMock;
    private readonly Mock<IRetailStockService> _retailStockMock;
    private readonly DeleteInventoryItemCommandHandler _sut;
    private readonly DeleteInventoryItemCommand _deleteCommand;
    private readonly int _successResponse;
    private readonly RetailStock? _retailStock;
    private readonly CancellationToken _token;
    private readonly int _failedResponse;
    public DeleteInventoryItemCommandHandlerTests()
    {
        IFixture fixture = new Fixture();
        //Mocks
        _serviceMock = fixture.Freeze<Mock<IInventoryItemService>>();
        _retailStockMock = new Mock<IRetailStockService>();
        _deleteCommand = fixture.Create<DeleteInventoryItemCommand>();
        _successResponse = 1;
        _retailStock = fixture.Create<RetailStock>();
        _token = fixture.Create<CancellationToken>();
        _failedResponse = -1;

        _sut = new DeleteInventoryItemCommandHandler(_serviceMock.Object, _retailStockMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldDeleteInventoryItem()
    {
        await _sut.Handle(_deleteCommand, _token);

        _serviceMock.Verify(s => s.DeleteInventoryItem(_deleteCommand.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeleteInventoryItemFails_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_failedResponse);

        var result = await _sut.Handle(_deleteCommand, _token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Handle_WhenDeleteInventoryItemThrowsError_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .Throws<Exception>();

        var result = await _sut.Handle(_deleteCommand, _token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Handle_WhenDeleteInventoryItemSuccessful_ShouldGetRetailStock()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);

        await _sut.Handle(_deleteCommand, _token);

        _retailStockMock.Verify(s => s.GetRetailStock(It.Is<string>(c =>
            c.Contains("InventoryItemId", StringComparison.InvariantCultureIgnoreCase)), _deleteCommand.Id));
    }

    [Fact]
    public async Task Handle_WhenGetRetailStockReturnsNull_ShouldNotCallDeleteRetailStock()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(() => null);

        await _sut.Handle(_deleteCommand, _token);

        _retailStockMock.Verify(s => s.DeleteRetailStock(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGetRetailStockThrowsError_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Exception thrown"));

        var result = await _sut.Handle(_deleteCommand, _token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }

    [Fact]
    public async Task Handle_WhenGetRetailStockSuccessful_ShouldDeleteRetailStock()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(_retailStock);

        await _sut.Handle(_deleteCommand, _token);

        _retailStockMock.Verify(r => r.DeleteRetailStock(_retailStock.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeleteRetailStockFails_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(_retailStock);
        _retailStockMock.Setup(s => s.DeleteRetailStock(It.IsAny<int>()))
            .ReturnsAsync(_failedResponse);

        var result = await _sut.Handle(_deleteCommand, _token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Handle_WhenDeleteRetailStockThrowsError_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(_retailStock);
        _retailStockMock.Setup(s => s.DeleteRetailStock(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Exception thrown"));

        var result = await _sut.Handle(_deleteCommand, _token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }

    [Fact]
    public async Task Handle_WhenAllOperationsSuccessful_ShouldReturnSuccessApiResponse()
    {
        _serviceMock.Setup(s => s.DeleteInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);
        _retailStockMock.Setup(s => s.GetRetailStock(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(_retailStock);
        _retailStockMock.Setup(s => s.DeleteRetailStock(It.IsAny<int>()))
            .ReturnsAsync(_successResponse);

        var result = await _sut.Handle(_deleteCommand, _token);

        _serviceMock.Verify(s => s.DeleteInventoryItem(_deleteCommand.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

}
