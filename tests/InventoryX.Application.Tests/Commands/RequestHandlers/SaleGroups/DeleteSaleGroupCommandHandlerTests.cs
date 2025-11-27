using InventoryX.Application.Commands.RequestHandlers.SaleGroups;
using InventoryX.Application.Commands.Requests.SaleGroups;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.SaleGroups;

public class DeleteSaleGroupCommandHandlerTests
{
    [Theory, AutoDomainData]
    public async Task Handle_WhenGetSaleGroupReturnsNull_ReturnFailedApiResponse(
        CancellationToken token,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        DeleteSaleGroupCommand command,
        DeleteSaleGroupCommandHandler sut)
    {
        saleGroupServiceMock.Setup(sS => sS.GetSaleGroup(command.Id).Result)
            .Returns(() => null);

        var result = await sut.Handle(command, token);

        saleGroupServiceMock.Verify(sS => sS.GetSaleGroup(command.Id), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Message.Should().Contain("not found");
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenGetSaleGroupThrowsException_ReturnFailedApiResponse(
        CancellationToken token,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        DeleteSaleGroupCommand command,
        DeleteSaleGroupCommandHandler sut)
    {
        saleGroupServiceMock.Setup(sS => sS.GetSaleGroup(command.Id).Result)
            .Throws<Exception>();

        var result = await sut.Handle(command, token);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenCalled_DeleteSaleGroup(
        CancellationToken token,
        SaleGroup saleGroup,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        DeleteSaleGroupCommand command,
        DeleteSaleGroupCommandHandler sut)
    {
        saleGroup.Id = command.Id;
        const int successResponse = 1;
        saleGroupServiceMock.Setup(sS => sS.GetSaleGroup(command.Id).Result)
            .Returns(saleGroup);

        saleGroupServiceMock.Setup(sS => sS.DeleteSaleGroup(saleGroup.Id).Result)
            .Returns(successResponse);


        var result = await sut.Handle(command, token);

        saleGroupServiceMock.Verify(sS => sS.DeleteSaleGroup(saleGroup.Id));
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenDeleteSaleGroupFails_ReturnFailedApiResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        DeleteSaleGroupCommand command,
        DeleteSaleGroupCommandHandler sut)
    {
        saleGroup.Id = command.Id;
        const int failedResponse = 0;
        saleGroupServiceMock.Setup(sS => sS.GetSaleGroup(command.Id).Result)
            .Returns(saleGroup);

        saleGroupServiceMock.Setup(sS => sS.DeleteSaleGroup(saleGroup.Id).Result)
            .Returns(failedResponse);


        var result = await sut.Handle(command, token);

        saleGroupServiceMock.Verify(sS => sS.DeleteSaleGroup(saleGroup.Id));
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenDeleteSaleGroupThrowsException_ReturnFailedApiResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        DeleteSaleGroupCommand command,
        DeleteSaleGroupCommandHandler sut)
    {
        saleGroup.Id = command.Id;
        saleGroupServiceMock.Setup(sS => sS.GetSaleGroup(command.Id).Result)
            .Returns(saleGroup);

        saleGroupServiceMock.Setup(sS => sS.DeleteSaleGroup(saleGroup.Id).Result)
            .Throws<Exception>();


        var result = await sut.Handle(command, token);

        saleGroupServiceMock.Verify(sS => sS.DeleteSaleGroup(saleGroup.Id));
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

}
