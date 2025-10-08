using InventoryX.Application.Queries.RequestHandlers.RetailStock;
using InventoryX.Application.Queries.Requests.RetailStock;
using InventoryX.Application.DTOs.RetailStock;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.RetailStock;

public class GetByInventoryItemRetailStockRequestHandlerTests
{

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenInventoryItemIdIsInvalid_ShouldReturnFailedApiResponse( 
        GetByInventoryItemRetailStockRequest request,
        GetByInventoryItemRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        request.Id = 0; 

        var result = await sut.Handle(request, ct);
        
        result.Should().NotBeNull();
        result.Success.Should().BeFalse(); 
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceReturnsNull_ShouldReturnFailedApiResponse(
        [Frozen] Mock<IRetailStockService> serviceMock,
        GetByInventoryItemRetailStockRequest request,
        GetByInventoryItemRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        serviceMock.Setup(s => s.GetRetailStock("InventoryItemId", request.Id))
            .ReturnsAsync((InventoryX.Domain.Models.RetailStock?)null);

        var result = await sut.Handle(request, ct);

        serviceMock.Verify(s => s.GetRetailStock("InventoryItemId", request.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse(); 
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceThrowsException_ShouldReturnFailedApiResponse(
        [Frozen] Mock<IRetailStockService> serviceMock,
        GetByInventoryItemRetailStockRequest request,
        GetByInventoryItemRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        serviceMock.Setup(s => s.GetRetailStock("InventoryItemId", request.Id))
            .ThrowsAsync(new Exception("Database error"));

        var result = await sut.Handle(request, ct);

        serviceMock.Verify(s => s.GetRetailStock("InventoryItemId", request.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Database error");
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenSuccessful_ShouldReturnSuccessApiResponse(
        InventoryX.Domain.Models.RetailStock retailStock,
        RetailStockDto retailStockDto,
        [Frozen] Mock<IRetailStockService> serviceMock,
        [Frozen] Mock<IMapper> mapperMock,
        GetByInventoryItemRetailStockRequest request,
        GetByInventoryItemRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        serviceMock.Setup(s => s.GetRetailStock("InventoryItemId", request.Id))
            .ReturnsAsync(retailStock);
        mapperMock.Setup(s => s.Map<RetailStockDto>(retailStock)).Returns(retailStockDto);

        var result = await sut.Handle(request, ct);

        serviceMock.Verify(s => s.GetRetailStock("InventoryItemId", request.Id), Times.Once);
        mapperMock.Verify(s => s.Map<RetailStockDto>(retailStock), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); 
        result.Body.Should().BeEquivalentTo(retailStockDto);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
