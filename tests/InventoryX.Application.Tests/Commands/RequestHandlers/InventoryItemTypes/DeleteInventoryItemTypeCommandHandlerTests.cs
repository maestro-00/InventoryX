using InventoryX.Application.Commands.RequestHandlers.InventoryItemTypes;
using InventoryX.Application.Commands.Requests.InventoryItemTypes;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.InventoryItemTypes;

public class DeleteInventoryItemTypeCommandHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenInventoryItemIdIsLessThanOne_Return400BadRequest(
        DeleteInventoryItemTypeCommand request,
        CancellationToken cancellationToken,
        [Frozen] Mock<IInventoryItemTypeService> serviceMock,
        DeleteInventoryItemTypeCommandHandler sut)
    {
        request.Id = 0;

        var result = await sut.Handle(request, cancellationToken);

        serviceMock.Verify(s => s.DeleteInventoryItemType(It.IsAny<int>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenDeleteInventoryItemFails_ReturnFailedResponse(
        DeleteInventoryItemTypeCommand request,
        CancellationToken cancellationToken,
        [Frozen] Mock<IInventoryItemTypeService> serviceMock,
        DeleteInventoryItemTypeCommandHandler sut)
    {
        const int failedResponse = -1;
        serviceMock.Setup(s => s.DeleteInventoryItemType(It.IsAny<int>()).Result)
            .Returns(failedResponse);

        var result = await sut.Handle(request, cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenDeleteInventoryItemThrowsError_ReturnFailedResponse(
        DeleteInventoryItemTypeCommand request,
        CancellationToken cancellationToken,
        [Frozen] Mock<IInventoryItemTypeService> serviceMock,
        DeleteInventoryItemTypeCommandHandler sut)
    {
        serviceMock.Setup(s => s.DeleteInventoryItemType(It.IsAny<int>()).Result)
            .Throws(new Exception("Exception Thrown"));

        var result = await sut.Handle(request, cancellationToken);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Message.Should().Contain("Exception");
    }

    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenDeleteInventoryItemSucceeds_ReturnSuccessResponse(
        DeleteInventoryItemTypeCommand request,
        CancellationToken cancellationToken,
        [Frozen] Mock<IInventoryItemTypeService> serviceMock,
        DeleteInventoryItemTypeCommandHandler sut)
    {
        const int successResponse = 1;
        serviceMock.Setup(s => s.DeleteInventoryItemType(It.IsAny<int>()).Result)
            .Returns(successResponse);

        var result = await sut.Handle(request, cancellationToken);

        serviceMock.Verify(s => s.DeleteInventoryItemType(It.IsAny<int>()), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

}
