using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Application.Queries.RequestHandlers.InventoryItemTypes;
using InventoryX.Application.Queries.Requests.InventoryItemTypes;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.InventoryItemTypes;

public class GetInventoryItemTypeRequestHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenRequestIdIsInvalid_ReturnBadRequest(
            Mock<IInventoryItemTypeService> serviceMock,
            GetInventoryItemTypeRequestHandler sut,
            GetInventoryItemTypeRequest request,
            CancellationToken token)
    {
            request.Id = 0;

            var result = await sut.Handle(request, token);
            
            serviceMock.Verify(s => s.GetInventoryItemType(It.IsAny<int>()),Times.Never);
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            result.Body.Should().BeNull();
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemIsNull_ReturnNotFoundResponse(
            [Frozen] Mock<IInventoryItemTypeService> serviceMock,
            GetInventoryItemTypeRequestHandler sut,
            GetInventoryItemTypeRequest request,
            CancellationToken token)
    {
            serviceMock.Setup(s => s.GetInventoryItemType(It.IsAny<int>()).Result)
                    .Returns((InventoryItemType?)null);
            
            var result = await sut.Handle(request, token);
            
            serviceMock.Verify(s => s.GetInventoryItemType(request.Id),Times.Once);
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            result.Body.Should().BeNull();
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemThrowsException_ReturnFailedResponse(
            [Frozen] Mock<IInventoryItemTypeService> serviceMock,
            GetInventoryItemTypeRequestHandler sut,
            GetInventoryItemTypeRequest request,
            CancellationToken token)
    {
            serviceMock.Setup(s => s.GetInventoryItemType(It.IsAny<int>()).Result)
                    .Throws(new Exception("Exception thrown"));
            
            var result = await sut.Handle(request, token);
            
            serviceMock.Verify(s => s.GetInventoryItemType(request.Id),Times.Once);
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            result.Body.Should().BeNull();
            result.Message.Should().Contain("Exception");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemSucceeds_ReturnSuccessResponse(
            InventoryItemType itemTypeMock, 
            GetInventoryItemTypeDto dtoMock, 
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IInventoryItemTypeService> serviceMock,
            GetInventoryItemTypeRequestHandler sut,
            GetInventoryItemTypeRequest request,
            CancellationToken token)
    {
            serviceMock.Setup(s => s.GetInventoryItemType(It.IsAny<int>()).Result)
                    .Returns(itemTypeMock);
            mapperMock.Setup(m => m.Map<GetInventoryItemTypeDto>(It.IsAny<InventoryItemType>()))
                    .Returns(dtoMock);
        
            var result = await sut.Handle(request, token);
            
            serviceMock.Verify(s => s.GetInventoryItemType(request.Id),Times.Once);
            mapperMock.Verify(m => m.Map<GetInventoryItemTypeDto>(itemTypeMock), Times.Once);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCodes.Status200OK);
            result.Body.Should().NotBeNull();
            result.Body.Should().BeEquivalentTo(dtoMock);
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ReturnFailedResponse(
            [Frozen] Mock<IMapper> mapperMock,
            GetInventoryItemTypeRequestHandler sut,
            GetInventoryItemTypeRequest request,
            CancellationToken token)
    {
            mapperMock.Setup(s => s.Map<GetInventoryItemTypeDto>(It.IsAny<InventoryItemType>()))
                    .Throws(new Exception("Exception thrown"));
            
            var result = await sut.Handle(request, token);
            
            mapperMock.Verify(s => s.Map<GetInventoryItemTypeDto>(It.IsAny<InventoryItemType>()),Times.Once);
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            result.Body.Should().BeNull();
            result.Message.Should().Contain("Exception");
    }
    
}