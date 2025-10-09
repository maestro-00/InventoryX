using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Application.Queries.RequestHandlers.InventoryItemTypes;
using InventoryX.Application.Queries.Requests.InventoryItemTypes;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.InventoryItemTypes;

public class GetAllInventoryItemTypeRequestHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemTypeThrowsException_ReturnFailedResponse(
            [Frozen] Mock<IInventoryItemTypeService> serviceMock,
            GetAllInventoryItemTypeRequest request,
            GetAllInventoryItemTypeRequestHandler sut,
            CancellationToken cancellationToken
        )
    {
        serviceMock.Setup(s => s.GetAllInventoryItemTypes().Result)
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(request, cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetInventoryItemTypeSucceeds_ReturnSuccessResponse(
            IEnumerable<InventoryItemType> inventoryItemTypes,
            IEnumerable<GetInventoryItemTypeDto> inventoryItemTypeDto,
            [Frozen] Mock<IInventoryItemTypeService> serviceMock,
            [Frozen] Mock<IMapper> mapperMock,
            GetAllInventoryItemTypeRequest request,
            GetAllInventoryItemTypeRequestHandler sut,
            CancellationToken cancellationToken
        )
    {
        serviceMock.Setup(s => s.GetAllInventoryItemTypes().Result)
            .Returns(inventoryItemTypes);
        mapperMock.Setup(m => m.Map<IEnumerable<GetInventoryItemTypeDto>>(inventoryItemTypes))
            .Returns(inventoryItemTypeDto);

        var result = await sut.Handle(request, cancellationToken);

        serviceMock.Verify(s => s.GetAllInventoryItemTypes(), Times.Once);
        mapperMock.Verify(m => m.Map<IEnumerable<GetInventoryItemTypeDto>>(inventoryItemTypes), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Body.Should().BeEquivalentTo(inventoryItemTypeDto);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperFails_ReturnFailedResponse(
            IEnumerable<InventoryItemType> inventoryItemTypes,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IInventoryItemTypeService> serviceMock,
            GetAllInventoryItemTypeRequest request,
            GetAllInventoryItemTypeRequestHandler sut,
            CancellationToken cancellationToken
        )
    {
        serviceMock.Setup(s => s.GetAllInventoryItemTypes().Result)
            .Returns(inventoryItemTypes);
        mapperMock.Setup(m => m.Map<IEnumerable<GetInventoryItemTypeDto>>(inventoryItemTypes))
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(request, cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }

}
