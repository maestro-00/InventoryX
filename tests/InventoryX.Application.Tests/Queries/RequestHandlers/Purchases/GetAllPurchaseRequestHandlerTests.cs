using InventoryX.Application.DTOs.Purchases;
using InventoryX.Application.Queries.RequestHandlers.Purchases;
using InventoryX.Application.Queries.Requests.Purchases;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.Purchases;

public class GetAllPurchaseRequestHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceReturnsPurchases_ShouldReturnSuccessResponse(
        [Frozen] Mock<IPurchaseService> purchaseServiceMock,
        [Frozen] Mock<IMapper> mapperMock,
        IEnumerable<Purchase> purchases,
        IEnumerable<GetPurchaseDto> purchaseDtos,
        GetAllPurchaseRequest request,
        GetAllPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseServiceMock.Setup(s => s.GetAllPurchases().Result).Returns(purchases);
        mapperMock.Setup(m => m.Map<IEnumerable<GetPurchaseDto>>(purchases)).Returns(purchaseDtos);

        var result = await sut.Handle(request, ct);

        purchaseServiceMock.Verify(s => s.GetAllPurchases(), Times.Once);
        mapperMock.Verify(m => m.Map<IEnumerable<GetPurchaseDto>>(purchases), Times.Once);
        result.Success.Should().BeTrue();
        result.Body.Should().BeEquivalentTo(purchaseDtos);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceReturnsEmpty_ShouldReturnSuccessResponse(
        [Frozen] Mock<IPurchaseService> purchaseServiceMock,
        GetAllPurchaseRequest request,
        GetAllPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseServiceMock.Setup(s => s.GetAllPurchases().Result).Returns([]);

        var result = await sut.Handle(request, ct);

        purchaseServiceMock.Verify(s => s.GetAllPurchases(), Times.Once);
        result.Success.Should().BeTrue();
        result.Body.Should().BeEquivalentTo(new List<GetPurchaseDto>());
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenServiceThrowsException_ShouldReturnFailedResponse(
        [Frozen] Mock<IPurchaseService> purchaseServiceMock,
        GetAllPurchaseRequest request,
        GetAllPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseServiceMock.Setup(s => s.GetAllPurchases().Result).Throws(new Exception("Service exception"));

        var result = await sut.Handle(request, ct);

        purchaseServiceMock.Verify(s => s.GetAllPurchases(), Times.Once);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Service exception");
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ShouldReturnFailedResponse(
        [Frozen] Mock<IPurchaseService> purchaseServiceMock,
        [Frozen] Mock<IMapper> mapperMock,
        IEnumerable<Purchase> purchases,
        GetAllPurchaseRequest request,
        GetAllPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseServiceMock.Setup(s => s.GetAllPurchases()).ReturnsAsync(purchases);
        mapperMock.Setup(m => m.Map<IEnumerable<GetPurchaseDto>>(purchases)).Throws(new Exception("Mapper exception"));

        var result = await sut.Handle(request, ct);

        purchaseServiceMock.Verify(s => s.GetAllPurchases(), Times.Once);
        mapperMock.Verify(m => m.Map<IEnumerable<GetPurchaseDto>>(purchases), Times.Once);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Mapper exception");
        result.Body.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
