using InventoryX.Application.Commands.Requests.InventoryItemTypes;
using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Application.Queries.Requests.InventoryItemTypes;

namespace InventoryX.Presentation.Tests.Controllers;

public class InventoryItemTypesControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly InventoryItemTypesController _sut;
    private ApiResponse? _mockApiResponse;

    public InventoryItemTypesControllerTests()
    {
        _fixture = new Fixture();
        _mediatorMock = _fixture.Freeze<Mock<IMediator>>();
        _sut = new InventoryItemTypesController(_mediatorMock.Object);
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
    .Setup(x => x.Send(It.IsAny<GetInventoryItemTypeRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Get(mockId);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetInventoryItemTypeRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
    .Setup(x => x.Send(It.IsAny<GetAllInventoryItemTypeRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.GetAll();

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllInventoryItemTypeRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
        var itemTypeCommandDto = _fixture.Create<InventoryItemTypeCommandDto>();
        _mediatorMock
    .Setup(x => x.Send(
        It.IsAny<CreateInventoryItemTypeCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Add(itemTypeCommandDto);

        //Assert 
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
        _mediatorMock.Verify(x => x.Send(It.IsAny<CreateInventoryItemTypeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Add_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
    {
        //Arrange 
        var itemTypeCommandDtoMock = _fixture.Create<InventoryItemTypeCommandDto>();
        _sut.ModelState.AddModelError("Name", "Required");
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<CreateInventoryItemTypeCommand>()
                , It.IsAny<CancellationToken>()))!
            .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Add(itemTypeCommandDtoMock);

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
        var itemTypeCommandDtoMock = _fixture.Create<InventoryItemTypeCommandDto>();
        var id = _fixture.Create<int>();
        _mediatorMock
    .Setup(x => x.Send(
        It.IsAny<UpdateInventoryItemTypeCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Update(id, itemTypeCommandDtoMock);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateInventoryItemTypeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task Update_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
    {
        //Arrange 
        var commandDtoMock = _fixture.Create<InventoryItemTypeCommandDto>();
        var id = _fixture.Create<int>();
        _sut.ModelState.AddModelError("Name", "Required");
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<UpdateInventoryItemTypeCommand>()
                , It.IsAny<CancellationToken>()))!
            .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Update(id, commandDtoMock);

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
        It.IsAny<DeleteInventoryItemTypeCommand>()
        , It.IsAny<CancellationToken>()))
    .ReturnsAsync(_mockApiResponse);

        //Act
        var result = await _sut.Delete(id);

        //Assert 
        _mediatorMock.Verify(x => x.Send(It.IsAny<DeleteInventoryItemTypeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult?.StatusCode.Should().Be(statusCode);
    }

}
