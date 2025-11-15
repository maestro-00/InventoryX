using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.Queries.RequestHandlers.InventoryItems;
using InventoryX.Application.Queries.Requests.InventoryItems;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.InventoryItems;

public class GetAllInventoryItemRequestHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IInventoryItemService> _serviceMock;
    private readonly GetAllInventoryItemRequestHandler _sut;
    private readonly Mock<IMapper>? _mapperMock;
    private readonly GetAllInventoryItemRequest _getAllCommand;
    private readonly CancellationToken _token;

    public GetAllInventoryItemRequestHandlerTests()
    {
        _fixture = new Fixture();
        //Mocks
        _serviceMock = _fixture.Freeze<Mock<IInventoryItemService>>();
        _mapperMock = _fixture.Freeze<Mock<IMapper>>();
        Mock<IRetailStockService> retailServiceMock = new Mock<IRetailStockService>();
        _getAllCommand = _fixture.Create<GetAllInventoryItemRequest>();
        _token = _fixture.Create<CancellationToken>();

        _sut = new GetAllInventoryItemRequestHandler(_serviceMock.Object,retailServiceMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenGetAllInventoryItemsEmpty_ShouldReturnSuccessApiResponse()
    {
        var emptyItems = new List<InventoryItem>();
        _serviceMock.Setup(s => s.GetAllInventoryItems())
            .ReturnsAsync(emptyItems);

        var result = await _sut.Handle(_getAllCommand, _token);

        _serviceMock.Verify(s => s.GetAllInventoryItems(), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Success.Should().BeTrue();
        result.Body.Should().BeEquivalentTo(emptyItems);
    }

    [Fact]
    public async Task Handle_WhenGetAllInventoryItemsThrowsException_ShouldReturnFailedApiResponse()
    {
        _serviceMock.Setup(s => s.GetAllInventoryItems())
            .ThrowsAsync(new("Exception thrown"));

        var result = await _sut.Handle(_getAllCommand, _token);

        _serviceMock.Verify(s => s.GetAllInventoryItems(), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Exception");
        result.Body.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenGetAllInventoryItemsSuccessful_ShouldMapCorrectEntity()
    {
        var inventoryItems = _fixture.Create<List<InventoryItem>>();
        _serviceMock.Setup(s => s.GetAllInventoryItems())
            .ReturnsAsync(inventoryItems);

        await _sut.Handle(_getAllCommand, _token);

        _mapperMock.Verify(s => s.Map<IEnumerable<GetInventoryItemDto>>(inventoryItems), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenGetAllInventoryItemsSuccessful_ShouldReturnSuccessApiResponse()
    {
        var inventoryItems = _fixture.Create<List<InventoryItem>>();
        var inventoryItemDtos = _fixture.Create<List<GetInventoryItemDto>>();
        _mapperMock.Setup(s => s.Map<IEnumerable<GetInventoryItemDto>>(inventoryItems))
            .Returns(inventoryItemDtos);
        _serviceMock.Setup(s => s.GetAllInventoryItems())
            .ReturnsAsync(inventoryItems);

        var result = await _sut.Handle(_getAllCommand, _token);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Success.Should().BeTrue();
        result.Body.Should().BeEquivalentTo(inventoryItemDtos);
    }
        [Theory, AutoDomainData]
        public async Task Handle_WhenGetAllInventoryItemsSuccessful_ShouldFetchRetailQuantityOfEachInventoryItem(
            GetAllInventoryItemRequest inventoryItemRequest,
            CancellationToken token,
            IFixture fixture,
            [Frozen] Mock<IRetailStockService> retailStockServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            GetAllInventoryItemRequestHandler sut)
        {
            var inventoryItemDtos = fixture.CreateMany<GetInventoryItemDto>(2).ToList();
            mapperMock.Setup(m => m.Map<IEnumerable<GetInventoryItemDto>>(It.IsAny<IEnumerable<InventoryItem>>()))
                .Returns(inventoryItemDtos);

            var result = await sut.Handle(inventoryItemRequest, token);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.Status200OK);
            result.Success.Should().BeTrue();
            retailStockServiceMock.Verify(rss => rss.GetRetailStock("InventoryItemId", It.IsAny<int>()), Times.Exactly(inventoryItemDtos.Count));
        }

        [Theory, AutoDomainData]
        public async Task Handle_WhenGetRetailStockThrowsException_ShouldReturnFailedResponse(
            GetAllInventoryItemRequest inventoryItemRequest,
            CancellationToken token,
            IFixture fixture,
            [Frozen] Mock<IRetailStockService> retailStockServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            GetAllInventoryItemRequestHandler sut)
        {
            var inventoryItemDtos = fixture.CreateMany<GetInventoryItemDto>(1).ToList();
            mapperMock.Setup(m => m.Map<IEnumerable<GetInventoryItemDto>>(It.IsAny<IEnumerable<InventoryItem>>()))
                .Returns(inventoryItemDtos);
            retailStockServiceMock.Setup(rss => rss.GetRetailStock("InventoryItemId", It.IsAny<int>()).Result)
                .Throws<Exception>();

            var result = await sut.Handle(inventoryItemRequest, token);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            result.Success.Should().BeFalse();
            retailStockServiceMock.Verify(rss => rss.GetRetailStock("InventoryItemId", It.IsAny<int>()), Times.AtLeastOnce);
        }

}
