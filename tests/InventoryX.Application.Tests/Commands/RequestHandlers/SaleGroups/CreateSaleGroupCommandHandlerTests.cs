using InventoryX.Application.Commands.RequestHandlers.SaleGroups;
using InventoryX.Application.Commands.Requests.SaleGroups;
using InventoryX.Application.DTOs.SaleGroups;
using InventoryX.Application.DTOs.Sales;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.SaleGroups;

public class CreateSaleGroupCommandHandlerTests
{
    [Theory, AutoDomainData]
    public async Task Handle_WhenCalled_AddSaleGroup(
        CancellationToken token,
        SaleGroup saleGroup,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        [Frozen] Mock<ISaleService> saleServiceMock,
        [Frozen] Mock<IRetailStockService> retailStockServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        const int successResponse = 1;
        mapperMock.Setup(m => m.Map<SaleGroup>(saleGroupCommand.SaleGroupCommandDto))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Returns(successResponse);
        saleServiceMock.Setup(sS => sS.AddSale(It.IsAny<Sale>()).Result)
            .Returns(successResponse);
        retailStockServiceMock.Setup(sS => sS.UpdateRetailStock(It.IsAny<RetailStock>()).Result)
            .Returns(successResponse);

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        saleGroupServiceMock.Verify(x => x.AddSaleGroup(It.Is<SaleGroup>(s =>
            s == saleGroup &&
            s.Created_At != null && s.Created_At.Value.Date == DateTime.UtcNow.Date)), Times.Once);
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Success.Should().BeTrue();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenCalled_AddSalesOfSaleGroup(
        CancellationToken token,
        SaleGroup saleGroup,
        Sale sale,
        List<SaleCommandDto> saleDtos,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        [Frozen] Mock<ISaleService> saleServiceMock,
        [Frozen] Mock<IRetailStockService> retailStockServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        saleGroupCommand.SaleGroupCommandDto.Sales = saleDtos;
        const int successResponse = 1;
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Returns(successResponse);
        mapperMock.Setup(m => m.Map<Sale>(It.IsIn<SaleCommandDto>(saleDtos)))
            .Returns(sale);
        saleServiceMock.Setup(sS => sS.AddSale(sale).Result)
            .Returns(successResponse);
        retailStockServiceMock.Setup(sS => sS.UpdateRetailStock(It.IsAny<RetailStock>()).Result)
            .Returns(successResponse);

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        saleServiceMock.Verify(x => x.AddSale(It.Is<Sale>(s =>
            s == sale &&
            s.Created_At != null && s.Created_At.Value.Date == DateTime.UtcNow.Date &&
            s.SaleGroupId == saleGroup.Id)), Times.Exactly(saleDtos.Count));

        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Success.Should().BeTrue();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenSaleGroupMappingFails_ReturnFailedResponse(
        CancellationToken token,
        [Frozen] Mock<IMapper> mapperMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Throws<Exception>();

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        mapperMock.Verify(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()), Times.Once);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenSaleMappingFails_ReturnFailedResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        List<SaleCommandDto> saleDtos,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        saleGroupCommand.SaleGroupCommandDto.Sales = saleDtos;
        const int successResponse = 1;
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Returns(successResponse);
        mapperMock.Setup(m => m.Map<Sale>(It.IsIn<SaleCommandDto>(saleDtos)))
            .Throws<Exception>();

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        mapperMock.Verify(m => m.Map<Sale>(It.IsIn<SaleCommandDto>(saleDtos)), Times.AtLeastOnce);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenAddSaleGroupFails_ReturnFailedResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        const int failedResponse = 0;
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Returns(failedResponse);

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        saleGroupServiceMock.Verify(sG => sG.AddSaleGroup(saleGroup), Times.Once);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenAddSaleGroupThrowsException_ReturnFailedResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Throws<Exception>();

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        saleGroupServiceMock.Verify(sG => sG.AddSaleGroup(saleGroup), Times.Once);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenAddSaleFails_ReturnFailedApiResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        Sale sale,
        List<SaleCommandDto> saleDtos,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        [Frozen] Mock<ISaleService> saleServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        saleGroupCommand.SaleGroupCommandDto.Sales = saleDtos;
        const int successResponse = 1;
        const int failedResponse = 0;
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Returns(successResponse);
        mapperMock.Setup(m => m.Map<Sale>(It.IsIn<SaleCommandDto>(saleDtos)))
            .Returns(sale);
        saleServiceMock.Setup(sS => sS.AddSale(sale).Result)
            .Returns(failedResponse);

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        saleServiceMock.Verify(sS => sS.AddSale(sale), Times.AtLeastOnce);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

    [Theory, AutoDomainData]
    public async Task Handle_WhenAddSaleThrowsException_ReturnFailedApiResponse(
        CancellationToken token,
        SaleGroup saleGroup,
        Sale sale,
        List<SaleCommandDto> saleDtos,
        [Frozen] Mock<IMapper> mapperMock,
        [Frozen] Mock<ISaleGroupService> saleGroupServiceMock,
        [Frozen] Mock<ISaleService> saleServiceMock,
        CreateSaleGroupCommand saleGroupCommand,
        CreateSaleGroupCommandHandler sut
        )
    {
        saleGroupCommand.SaleGroupCommandDto.Sales = saleDtos;
        const int successResponse = 1;
        const int failedResponse = 0;
        mapperMock.Setup(m => m.Map<SaleGroup>(It.IsAny<SaleGroupCommandDto>()))
            .Returns(saleGroup);
        saleGroupServiceMock.Setup(sG => sG.AddSaleGroup(saleGroup).Result)
            .Returns(successResponse);
        mapperMock.Setup(m => m.Map<Sale>(It.IsIn<SaleCommandDto>(saleDtos)))
            .Returns(sale);
        saleServiceMock.Setup(sS => sS.AddSale(sale).Result)
            .Throws<Exception>();

        ApiResponse result = await sut.Handle(saleGroupCommand, token);

        saleServiceMock.Verify(sS => sS.AddSale(sale), Times.AtLeastOnce);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
    }

}
