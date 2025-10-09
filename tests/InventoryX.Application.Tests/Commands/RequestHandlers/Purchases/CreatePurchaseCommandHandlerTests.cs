using InventoryX.Application.Commands.RequestHandlers.Purchases;
using InventoryX.Application.Commands.Requests.Purchases;
using InventoryX.Application.DTOs.Purchases;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.Purchases;

public class CreatePurchaseCommandHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ShouldReturnFailedResponse(
            [Frozen] Mock<IMapper> mapperMock,
            CreatePurchaseCommand command,
            CreatePurchaseCommandHandler sut,
            CancellationToken token
        )
    {
        mapperMock.Setup(m => m.Map<Purchase>(It.IsAny<PurchaseCommandDto>()))
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(command, token);


        mapperMock.Verify(s => s.Map<Purchase>(command.NewPurchaseDto), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenCreatePurchaseFails_ShouldReturnFailedResponse(
            [Frozen] Mock<IPurchaseService> purchaseMock,
            CreatePurchaseCommand command,
            CreatePurchaseCommandHandler sut,
            CancellationToken token
        )
    {
        var failedResponse = 0;
        purchaseMock.Setup(m => m.AddPurchase(It.IsAny<Purchase>()).Result)
            .Returns(failedResponse);

        var result = await sut.Handle(command, token);

        purchaseMock.Verify(s => s.AddPurchase(It.IsAny<Purchase>()), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenCreatePurchaseThrowsException_ShouldReturnFailedResponse(
            [Frozen] Mock<IPurchaseService> purchaseMock,
            CreatePurchaseCommand command,
            CreatePurchaseCommandHandler sut,
            CancellationToken token
        )
    {
        purchaseMock.Setup(m => m.AddPurchase(It.IsAny<Purchase>()).Result)
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(command, token);

        purchaseMock.Verify(s => s.AddPurchase(It.IsAny<Purchase>()), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenCreatePurchaseSucceeds_ShouldReturnSuccessResponse(
            Purchase purchase,
            [Frozen] Mock<IPurchaseService> purchaseMock,
            [Frozen] Mock<IMapper> mapperMock,
            CreatePurchaseCommand command,
            CreatePurchaseCommandHandler sut,
            CancellationToken token
        )
    {
        var newlyCreatedId = 1;
        purchaseMock.Setup(m => m.AddPurchase(It.IsAny<Purchase>()).Result)
            .Returns(newlyCreatedId);
        mapperMock.Setup(m => m.Map<Purchase>(It.IsAny<PurchaseCommandDto>()))
            .Returns(purchase);

        var result = await sut.Handle(command, token);

        mapperMock.Verify(s => s.Map<Purchase>(command.NewPurchaseDto), Times.Once);
        purchaseMock.Verify(p => p.AddPurchase(It.Is<Purchase>(
                pItem => pItem == purchase &&
                         pItem.Created_At != null &&
                         pItem.Created_At.Value.Date == DateTime.UtcNow.Date
            )), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Id.Should().Be(newlyCreatedId);
    }

}
