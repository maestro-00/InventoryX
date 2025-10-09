using InventoryX.Application.Commands.RequestHandlers.Purchases;
using InventoryX.Application.Commands.Requests.Purchases;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.Purchases;

public class DeletePurchaseCommandHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenPurchaseIdIsInvalid_ReturnInvalidIdResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        DeletePurchaseCommand command,
        DeletePurchaseCommandHandler sut,
        CancellationToken ct)
    {
        command.Id = 0;

        var result = await sut.Handle(command, ct);

        purchaseMock.Verify(p => p.DeletePurchase(It.IsAny<int>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenDeletePurchaseSuccessful_ReturnSuccessResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        DeletePurchaseCommand command,
        DeletePurchaseCommandHandler sut,
        CancellationToken ct)
    {
        var successResponse = 1;
        purchaseMock.Setup(p => p.DeletePurchase(It.IsAny<int>()).Result)
            .Returns(successResponse);

        var result = await sut.Handle(command, ct);

        purchaseMock.Verify(p => p.DeletePurchase(command.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenDeletePurchaseThrowsException_ReturnNotFoundResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        DeletePurchaseCommand command,
        DeletePurchaseCommandHandler sut,
        CancellationToken ct)
    {
        purchaseMock.Setup(p => p.DeletePurchase(It.IsAny<int>()).Result)
            .Throws(new Exception("Exception thrown"));

        var result = await sut.Handle(command, ct);

        purchaseMock.Verify(p => p.DeletePurchase(command.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenDeletePurchaseFails_ReturnFailedResponse(
        [Frozen] Mock<IPurchaseService> purchaseMock,
        DeletePurchaseCommand command,
        DeletePurchaseCommandHandler sut,
        CancellationToken ct)
    {
        var failedResponse = 0;
        purchaseMock.Setup(p => p.DeletePurchase(It.IsAny<int>()).Result)
            .Returns(failedResponse);

        var result = await sut.Handle(command, ct);

        purchaseMock.Verify(p => p.DeletePurchase(command.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

}
