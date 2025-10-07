using InventoryX.Application.Commands.RequestHandlers.InventoryItemTypes;
using InventoryX.Application.Commands.Requests.InventoryItemTypes;
using InventoryX.Application.DTOs.InventoryItemTypes;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.InventoryItemTypes;

public class UpdateInventoryItemTypeCommandHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IInventoryItemTypeService> _serviceMock; 
    private readonly UpdateInventoryItemTypeCommandHandler _sut;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UpdateInventoryItemTypeCommand _updateCommand;
    private readonly int _successResponse; 
    private readonly CancellationToken _token;
    private readonly int _failedResponse;

    public UpdateInventoryItemTypeCommandHandlerTests()
    {
        _fixture = new Fixture();
        //Mocks
        _serviceMock = _fixture.Freeze<Mock<IInventoryItemTypeService>>();
        _mapperMock = _fixture.Freeze<Mock<IMapper>>();  
        _updateCommand = _fixture.Create<UpdateInventoryItemTypeCommand>();
        _successResponse = 1; 
        _token = _fixture.Create<CancellationToken>();
        _failedResponse = 0;
        
        _sut = new UpdateInventoryItemTypeCommandHandler(_serviceMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldMapDtoToInventoryItemType()
    {
        await _sut.Handle(_updateCommand, _token);
        
        _mapperMock.Verify(x => x.Map<InventoryItemType>(_updateCommand.InventoryItemTypeDto), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WhenMapperFails_ShouldReturnFailedResponse()
    {
        _mapperMock.Setup(x => x.Map<InventoryItemType>(It.IsAny<InventoryItemTypeCommandDto>()))
            .Throws<Exception>();
        
        var result = await _sut.Handle(_updateCommand, _token);
        
        _mapperMock.Verify(x => x.Map<InventoryItemType>(It.IsAny<InventoryItemTypeCommandDto>()), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
        result.Id.Should().BeNull();
    }
    
    [Fact]
    public async Task Handle_WhenMapperReturnsNull_ShouldReturnFailedResponse()
    {
         _mapperMock.Setup(x => x.Map<InventoryItemType>(It.IsAny<InventoryItemTypeCommandDto>()))
            .Returns(() => null);
        
        var result = await _sut.Handle(_updateCommand, _token);
        
        _mapperMock.Verify(x => x.Map<InventoryItemType>(It.IsAny<InventoryItemTypeCommandDto>()), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
        result.Id.Should().BeNull();
    }
    
    [Fact]
    public async Task Handle_WhenUpdateInventoryItemTypeFails_ShouldReturnFailedResponse()
    {
        _serviceMock.Setup(x => x.UpdateInventoryItemType(It.IsAny<InventoryItemType>()))
            .ReturnsAsync(_failedResponse);
        
        var result = await _sut.Handle(_updateCommand, _token);
        
        _serviceMock.Verify(x => x.UpdateInventoryItemType(It.IsAny<InventoryItemType>()), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
        result.Id.Should().BeNull();
    }
    
    [Fact]
    public async Task Handle_WhenUpdateInventoryItemTypeThrowsError_ShouldReturnFailedResponse()
    {
        _serviceMock.Setup(x => x.UpdateInventoryItemType(It.IsAny<InventoryItemType>()))
            .ThrowsAsync(new Exception("Exception thrown"));
        
        var result = await _sut.Handle(_updateCommand, _token);
        
        _serviceMock.Verify(x => x.UpdateInventoryItemType(It.IsAny<InventoryItemType>()), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Success.Should().BeFalse();
        result.Id.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }
    
    [Fact]
    public async Task Handle_WhenUpdateInventoryItemTypeSuccessful_ShouldReturnSuccessResponse()
    {
        var inventoryItemType = _fixture.Create<InventoryItemType>();
        _mapperMock.Setup(x => x.Map<InventoryItemType>(It.IsAny<InventoryItemTypeCommandDto>()))
            .Returns(inventoryItemType);
        _serviceMock.Setup(x => x.UpdateInventoryItemType(It.IsAny<InventoryItemType>()))
            .ReturnsAsync(_successResponse);
        
        var result = await _sut.Handle(_updateCommand, _token);
        
        _serviceMock.Verify(s => s.UpdateInventoryItemType(It.Is<InventoryItemType>(i => 
            i.Name == inventoryItemType.Name &&
            i.Id == _updateCommand.Id &&
            i.Updated_At != null &&
            i.Updated_At.Value.Date == DateTime.UtcNow.Date
            )), Times.Once);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        result.Success.Should().BeTrue();
        result.Id.Should().Be(_updateCommand.Id);
    }
    
}