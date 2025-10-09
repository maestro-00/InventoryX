using InventoryX.Application.Queries.RequestHandlers.RetailStock;
using InventoryX.Application.Queries.Requests.RetailStock;
using InventoryX.Application.DTOs.RetailStock;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.RetailStock;

public class GetAllRetailStockRequestHandlerTests
{
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceThrowsException_ShouldReturnFailedApiResponse(
        [Frozen] Mock<IRetailStockService> serviceMock,
        GetAllRetailStockRequest request,
        GetAllRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        serviceMock.Setup(s => s.GetAllRetailStock())
            .ThrowsAsync(new Exception("Test exception"));

        var result = await sut.Handle(request, ct);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Test exception");
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceReturnsEmpty_ShouldReturnSuccessApiResponse(
        [Frozen] Mock<IRetailStockService> serviceMock,
        GetAllRetailStockRequest request,
        GetAllRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        serviceMock.Setup(s => s.GetAllRetailStock().Result)
            .Returns([]);

        var result = await sut.Handle(request, ct);

        serviceMock.Verify(s => s.GetAllRetailStock(), Times.Once); 
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();  
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ShouldReturnFailedApiResponse(
        [Frozen] Mock<IMapper> mapperMock,
        GetAllRetailStockRequest request,
        GetAllRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        mapperMock.Setup(s => s.Map<IEnumerable<RetailStockDto>>(It.IsAny<IEnumerable<Domain.Models.RetailStock>>()))
            .Throws(new Exception("Test exception"));

        var result = await sut.Handle(request, ct);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Test exception");
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenSuccessful_ShouldReturnSuccessApiResponse(
        List<InventoryX.Domain.Models.RetailStock> retailStocks,
        List<RetailStockDto> retailStockDtos,
        [Frozen] Mock<IRetailStockService> serviceMock,
        [Frozen] Mock<IMapper> mapperMock,
        GetAllRetailStockRequest request,
        GetAllRetailStockRequestHandler sut,
        CancellationToken ct)
    {
        serviceMock.Setup(s => s.GetAllRetailStock()).ReturnsAsync(retailStocks);
        mapperMock.Setup(s => s.Map<IEnumerable<RetailStockDto>>(retailStocks)).Returns(retailStockDtos);
        
        var result = await sut.Handle(request, ct);

        serviceMock.Verify(s => s.GetAllRetailStock(), Times.Once);
        mapperMock.Verify(s => s.Map<IEnumerable<RetailStockDto>>(retailStocks), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); 
        result.Body.Should().BeEquivalentTo(retailStockDtos);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
