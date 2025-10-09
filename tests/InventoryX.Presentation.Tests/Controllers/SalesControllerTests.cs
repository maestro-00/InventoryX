using InventoryX.Application.Commands.Requests.Sales;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Application.Queries.Requests.Sales;

namespace InventoryX.Presentation.Tests.Controllers;

public class SalesControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SalesController _sut;
    private ApiResponse? _mockApiResponse;

    public SalesControllerTests()
    {
        _fixture = new Fixture();
        _mediatorMock = _fixture.Freeze<Mock<IMediator>>();
        _sut = new SalesController(_mediatorMock.Object);
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
        .Setup(x => x.Send(It.IsAny<GetSaleRequest>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Get(mockId);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetSaleRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
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
    .Setup(x => x.Send(It.IsAny<GetAllSaleRequest>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.GetAll();

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllSaleRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

    [Theory]
    [InlineData(201)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Add_WhenCalledWithValidModelState_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        var saleCommandDto = _fixture.Create<SaleCommandDto>();
        _mediatorMock
    .Setup(x => x.Send(
        It.IsAny<CreateSaleCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Add(saleCommandDto);

        //Assert 
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
        _mediatorMock.Verify(x => x.Send(It.IsAny<CreateSaleCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Add_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
    {
        //Arrange 
        var saleCommandDto = _fixture.Create<SaleCommandDto>();
        _sut.ModelState.AddModelError("Quantity", "Required");
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<CreateSaleCommand>()
                , It.IsAny<CancellationToken>()))!
            .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Add(saleCommandDto);

        //Assert 
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult?.StatusCode.Should().Be(400);
    }

    [Theory]
    [InlineData(202)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Update_WhenCalledWithValidModelState_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        var saleCommandDto = _fixture.Create<SaleCommandDto>();
        var id = _fixture.Create<int>();
        _mediatorMock
    .Setup(x => x.Send(
        It.IsAny<UpdateSaleCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Update(id, saleCommandDto);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateSaleCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task Update_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
    {
        //Arrange 
        var saleCommandDto = _fixture.Create<SaleCommandDto>();
        var id = _fixture.Create<int>();
        _sut.ModelState.AddModelError("Quantity", "Required");
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<UpdateSaleCommand>()
                , It.IsAny<CancellationToken>()))!
            .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Update(id, saleCommandDto);

        //Assert 
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult?.StatusCode.Should().Be(400);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Delete_WhenCalledWithValidModelState_ShouldReturnRightActionResult(int statusCode)
    {
        //Arrange
        _mockApiResponse.StatusCode = statusCode;
        var id = _fixture.Create<int>();
        _mediatorMock
    .Setup(x => x.Send(
        It.IsAny<DeleteSaleCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Delete(id);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<DeleteSaleCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

}
