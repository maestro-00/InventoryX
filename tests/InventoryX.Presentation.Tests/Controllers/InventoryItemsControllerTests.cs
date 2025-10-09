using InventoryX.Application.Commands.Requests.InventoryItems;
using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.Queries.Requests.InventoryItems;

namespace InventoryX.Presentation.Tests.Controllers
{
    public class InventoryItemsControllerTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly InventoryItemsController _sut;
        private ApiResponse? _mockApiResponse;

        public InventoryItemsControllerTests()
        {
            _fixture = new Fixture();
            _mediatorMock = _fixture.Freeze<Mock<IMediator>>();
            _sut = new InventoryItemsController(_mediatorMock.Object);
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
        .Setup(x => x.Send(It.IsAny<GetInventoryItemRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.Get(mockId);

            //Assert 
            _mediatorMock.Verify(x => x.Send(It.IsAny<GetInventoryItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
        .Setup(x => x.Send(It.IsAny<GetAllInventoryItemRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.GetAll();

            //Assert 
            _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllInventoryItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
            var itemCommandDtoMock = _fixture.Create<InventoryItemCommandDto>();
            var retailQtyMock = _fixture.Create<decimal>();
            _mediatorMock
        .Setup(x => x.Send(
            It.IsAny<CreateInventoryItemCommand>()
            , It.IsAny<CancellationToken>()))
        .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.Add(itemCommandDtoMock, retailQtyMock);

            //Assert 
            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult?.StatusCode.Should().Be(statusCode);
            _mediatorMock.Verify(x => x.Send(It.IsAny<CreateInventoryItemCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Add_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
        {
            //Arrange 
            var itemCommandDtoMock = _fixture.Create<InventoryItemCommandDto>();
            var retailQtyMock = _fixture.Create<decimal>();
            _sut.ModelState.AddModelError("Name", "Required");
            _mediatorMock
                .Setup(x => x.Send(
                    It.IsAny<CreateInventoryItemCommand>()
                    , It.IsAny<CancellationToken>()))!
                .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.Add(itemCommandDtoMock, retailQtyMock);

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
            var itemCommandDtoMock = _fixture.Create<InventoryItemCommandDto>();
            var id = _fixture.Create<int>();
            _mediatorMock
        .Setup(x => x.Send(
            It.IsAny<UpdateInventoryItemCommand>()
            , It.IsAny<CancellationToken>()))
        .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.Update(id, itemCommandDtoMock);

            //Assert 
            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateInventoryItemCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult?.StatusCode.Should().Be(statusCode);
        }
        [Fact]
        public async Task Update_WhenCalledWithInvalidModelState_ShouldReturnBadRequest()
        {
            //Arrange 
            var itemCommandDtoMock = _fixture.Create<InventoryItemCommandDto>();
            var id = _fixture.Create<int>();
            _sut.ModelState.AddModelError("Name", "Required");
            _mediatorMock
                .Setup(x => x.Send(
                    It.IsAny<UpdateInventoryItemCommand>()
                    , It.IsAny<CancellationToken>()))!
                .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.Update(id, itemCommandDtoMock);

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
            It.IsAny<DeleteInventoryItemCommand>()
            , It.IsAny<CancellationToken>()))
        .ReturnsAsync(_mockApiResponse);

            //Act
            var result = await _sut.Delete(id);

            //Assert 
            _mediatorMock.Verify(x => x.Send(It.IsAny<DeleteInventoryItemCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult?.StatusCode.Should().Be(statusCode);
        }

    }
}
