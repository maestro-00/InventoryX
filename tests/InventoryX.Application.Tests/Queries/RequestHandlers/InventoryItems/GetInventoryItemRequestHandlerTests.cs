using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.Queries.RequestHandlers.InventoryItems;
using InventoryX.Application.Queries.Requests.InventoryItems;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.InventoryItems;

public class GetInventoryItemRequestHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IInventoryItemService> _serviceMock;
    private readonly Mock<IRetailStockService> _retailServiceMock;
    private readonly GetInventoryItemRequestHandler _sut;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetInventoryItemRequest _getRequest;
    private readonly CancellationToken _token;

    public GetInventoryItemRequestHandlerTests()
    {
        _fixture = new Fixture();
        //Mocks
        _serviceMock = _fixture.Freeze<Mock<IInventoryItemService>>();
        _retailServiceMock = new Mock<IRetailStockService>();
        _mapperMock = _fixture.Freeze<Mock<IMapper>>();
        _getRequest = _fixture.Create<GetInventoryItemRequest>();
        _token = _fixture.Create<CancellationToken>();

        _sut = new GetInventoryItemRequestHandler(_serviceMock.Object, _retailServiceMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenGetInventoryItemIsNull_ShouldReturnNotFoundResponse()
    {
        _serviceMock.Setup(s => s.GetInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.Handle(_getRequest, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Success.Should().BeFalse();
        result.Body.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenGetInventoryItemThrowsException_ShouldReturnFailedResponse()
    {
        _serviceMock.Setup(s => s.GetInventoryItem(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Exception thrown"));

        var result = await _sut.Handle(_getRequest, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
        result.Body.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }

    [Fact]
    public async Task Handle_WhenRequestIdIsLessThanOne_ShouldReturnInvalidIdResponse()
    {
        var request = _fixture.Create<GetInventoryItemRequest>();
        request.Id = 0;

        var result = await _sut.Handle(request, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Success.Should().BeFalse();
        result.Body.Should().BeNull();
        result.Message.ToLower().Should().Contain("invalid item id");
    }

    [Fact]
    public async Task Handle_WhenGetInventoryItemSuccessful_ShouldMapCorrectInventoryItem()
    {
        var inventoryItem = _fixture.Create<InventoryItem>();
        _serviceMock.Setup(s => s.GetInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(inventoryItem);

        await _sut.Handle(_getRequest, _token);

        _mapperMock.Verify(m => m.Map<GetInventoryItemDto>(inventoryItem), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenGetInventoryItemSuccessful_ShouldReturnSuccessResponse()
    {
        var inventoryItem = _fixture.Create<InventoryItem>();
        var inventoryItemDto = _fixture.Create<GetInventoryItemDto>();
        _mapperMock.Setup(m => m.Map<GetInventoryItemDto>(It.IsAny<InventoryItem>()))
            .Returns(inventoryItemDto);
        _serviceMock.Setup(s => s.GetInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(inventoryItem);

        var result = await _sut.Handle(_getRequest, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Success.Should().BeTrue();
        result.Body.Should().BeEquivalentTo(inventoryItemDto);
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenGetInventoryItemSuccessful_ShouldFetchInventoryRetailQuantity(
        GetInventoryItemRequest inventoryItemRequest,
        CancellationToken token,
        InventoryItem inventoryItem,
        GetInventoryItemDto inventoryItemDto,
        Domain.Models.RetailStock retailStock,
        [Frozen] Mock<IInventoryItemService> serviceMock,
        [Frozen] Mock<IRetailStockService> retailStockServiceMock,
        [Frozen] Mock<IMapper> mapperMock,
        GetInventoryItemRequestHandler sut)
    {
        mapperMock.Setup(m => m.Map<GetInventoryItemDto>(It.IsAny<InventoryItem>()))
            .Returns(inventoryItemDto);
        serviceMock.Setup(s => s.GetInventoryItem(It.IsAny<int>()))
            .ReturnsAsync(inventoryItem);
        retailStockServiceMock.Setup(rss => rss.GetRetailStock("InventoryItemId", inventoryItem.Id).Result)
            .Returns(retailStock);

        var result = await sut.Handle(inventoryItemRequest, token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Success.Should().BeTrue();
        result.Body.Should().BeEquivalentTo(inventoryItemDto);
        inventoryItemDto.RetailQuantity.Should().Be(retailStock?.Quantity ?? 0);
        retailStockServiceMock.Verify(rss => rss.GetRetailStock("InventoryItemId",inventoryItem.Id).Result, Times.Once);
    }

}
