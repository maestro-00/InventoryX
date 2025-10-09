using InventoryX.Application.DTOs.Purchases;
using InventoryX.Application.Queries.RequestHandlers.Purchases;
using InventoryX.Application.Queries.Requests.Purchases;

namespace InventoryX.Application.Tests.Queries.RequestHandlers.Purchases;

public class GetPurchaseRequestHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenIdIsInvalid_ShouldReturnFailedResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        GetPurchaseRequest request,
        GetPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        request.Id = 0;

        var result = await sut.Handle(request, ct);

        purchaseMock.Verify(p => p.GetPurchase(It.IsAny<int>()), Times.Never);
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Body.Should().BeNull();
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenPurchaseNotFound_ShouldReturnNotFoundResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        GetPurchaseRequest request,
        GetPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseMock.Setup(p => p.GetPurchase(It.IsAny<int>()).Result)
            .Returns((Purchase?)null);

        var result = await sut.Handle(request, ct);

        purchaseMock.Verify(p => p.GetPurchase(request.Id), Times.Once);
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Body.Should().BeNull();
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetPurchaseThrowsException_ShouldReturnFailedResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        GetPurchaseRequest request,
        GetPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseMock.Setup(p => p.GetPurchase(It.IsAny<int>()).Result)
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(request, ct);

        purchaseMock.Verify(p => p.GetPurchase(request.Id), Times.Once);
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Body.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenGetPurchaseSucceeds_ShouldReturnSuccessResponse(
        GetPurchaseDto purchaseDto,
        Purchase purchase,
        [Frozen] Mock<IPurchaseService> purchaseMock,
        [Frozen] Mock<IMapper> mapperMock,
        GetPurchaseRequest request,
        GetPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        purchaseMock.Setup(p => p.GetPurchase(It.IsAny<int>()).Result)
            .Returns(purchase);
        mapperMock.Setup(m => m.Map<GetPurchaseDto>(It.IsAny<Purchase>()))
            .Returns(purchaseDto);

        var result = await sut.Handle(request, ct);

        purchaseMock.Verify(p => p.GetPurchase(request.Id), Times.Once);
        mapperMock.Verify(m => m.Map<GetPurchaseDto>(purchase), Times.Once);
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Body.Should().BeEquivalentTo(purchaseDto);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ShouldReturnFailedResponse(
        [Frozen] Mock<IMapper> mapperMock,
        GetPurchaseRequest request,
        GetPurchaseRequestHandler sut,
        CancellationToken ct)
    {
        mapperMock.Setup(m => m.Map<GetPurchaseDto>(It.IsAny<Purchase>()))
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(request, ct);

        mapperMock.Verify(m => m.Map<GetPurchaseDto>(It.IsAny<Purchase>()), Times.Once);
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Body.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }

}
