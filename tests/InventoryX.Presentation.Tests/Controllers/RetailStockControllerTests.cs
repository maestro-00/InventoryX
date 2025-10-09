using InventoryX.Application.Commands.Requests.RetailStock;
using InventoryX.Application.DTOs.RetailStock;
using InventoryX.Application.Queries.Requests.RetailStock;

namespace InventoryX.Presentation.Tests.Controllers;

public class RetailStockControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly RetailStockController _sut;
    private ApiResponse? _mockApiResponse;

    public RetailStockControllerTests()
    {
        _fixture = new Fixture();
        _mediatorMock = _fixture.Freeze<Mock<IMediator>>();
        _sut = new RetailStockController(_mediatorMock.Object);
        _mockApiResponse = _fixture.Create<ApiResponse>();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task Get_WhenCalled_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        var mockId = _fixture.Create<int>();
        _mediatorMock
    .Setup(x => x.Send(It.IsAny<GetRetailStockRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Get(mockId);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetRetailStockRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        var objResult = result as ObjectResult;
        objResult.Should().NotBeNull();
        objResult?.StatusCode.Should().Be(statusCode);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task GetByInventoryItem_WhenCalled_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        var mockId = _fixture.Create<int>();
        _mediatorMock
    .Setup(x => x.Send(It.IsAny<GetByInventoryItemRetailStockRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.GetByInventoryItem(mockId);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetByInventoryItemRetailStockRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        var objResult = result as ObjectResult;
        objResult.Should().NotBeNull();
        objResult?.StatusCode.Should().Be(statusCode);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task GetAll_WhenCalled_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        _mediatorMock
    .Setup(x => x.Send(It.IsAny<GetAllRetailStockRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.GetAll();

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllRetailStockRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

    [Theory]
    [InlineData(202)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Update_WhenCalledWithValidModelState_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        var retailStockCommandMockDto = _fixture.Create<RetailStockCommandDto>();
        _mediatorMock
    .Setup(x => x.Send(
        It.IsAny<UpdateRetailStockCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Update(retailStockCommandMockDto);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateRetailStockCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task Update_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
    {
        //Arrange 
        var retailStockCommandMockDto = _fixture.Create<RetailStockCommandDto>();
        _sut.ModelState.AddModelError("Quantity", "Required");
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<UpdateRetailStockCommand>()
                , It.IsAny<CancellationToken>()))!
            .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Update(retailStockCommandMockDto);

        //Assert 
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult?.StatusCode.Should().Be(400);
    }

}
