using InventoryX.Application.Commands.RequestHandlers.Purchases;
using InventoryX.Application.Commands.Requests.Purchases;
using InventoryX.Application.DTOs.Purchases;

namespace InventoryX.Application.Tests.Commands.RequestHandlers.Purchases;

public class UpdatePurchaseCommandHandlerTests
{
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenPurchaseIdIsInvalid_ShouldReturnBadRequest(
            [Frozen] Mock<IPurchaseService> purchaseMock,
            UpdatePurchaseCommand command,
            UpdatePurchaseCommandHandler sut,
            CancellationToken cancellationToken
        )
    {
        command.Id = 0;
        
        var result = await sut.Handle(command, cancellationToken);
        
        purchaseMock.Verify(p => p.UpdatePurchase(It.IsAny<Purchase>()), Times.Never);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Id.Should().BeNull(); 
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenMapperThrowsException_ShouldReturnFailedResponse(
            [Frozen] Mock<IMapper> mapperMock,
            UpdatePurchaseCommand command,
            UpdatePurchaseCommandHandler sut,
            CancellationToken cancellationToken
        )
    {
        mapperMock.Setup(m => m.Map<Purchase>(It.IsAny<PurchaseCommandDto>()))
            .Throws(new Exception("Exception"));
        
        var result = await sut.Handle(command, cancellationToken);
        
        mapperMock.Verify(m => m.Map<Purchase>(command.PurchaseDto), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenUpdatePurchaseFails_ShouldReturnFailedResponse(
            [Frozen] Mock<IPurchaseService> purchaseMock,
            UpdatePurchaseCommand command,
            UpdatePurchaseCommandHandler sut,
            CancellationToken cancellationToken
        )
    {
        var failedResponse = 0;
        purchaseMock.Setup(p => p.UpdatePurchase(It.IsAny<Purchase>()).Result)
            .Returns(failedResponse);
        var result = await sut.Handle(command, cancellationToken);
        
        purchaseMock.Verify(p => p.UpdatePurchase(It.IsAny<Purchase>()));
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull(); 
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenUpdatePurchaseThrowsException_ShouldReturnFailedResponse(
            [Frozen] Mock<IPurchaseService> purchaseMock,
            UpdatePurchaseCommand command,
            UpdatePurchaseCommandHandler sut,
            CancellationToken cancellationToken
        )
    { 
        purchaseMock.Setup(p => p.UpdatePurchase(It.IsAny<Purchase>()).Result)
            .Throws(new Exception("Exception thrown"));
        
        var result = await sut.Handle(command, cancellationToken);
        
        purchaseMock.Verify(p => p.UpdatePurchase(It.IsAny<Purchase>()));
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Id.Should().BeNull();
        result.Message.Should().Contain("Exception");
    }
    
    [Theory]
    [AutoDomainData]
    public async Task Handle_WhenUpdatePurchaseSucceeds_ShouldReturnBadRequest(
            Purchase purchase,
            [Frozen] Mock<IPurchaseService> purchaseMock,
            [Frozen] Mock<IMapper> mapperMock,
            UpdatePurchaseCommand command,
            UpdatePurchaseCommandHandler sut,
            CancellationToken cancellationToken
        )
    {
        var successResponse = 1;
        mapperMock.Setup(m => m.Map<Purchase>(It.IsAny<PurchaseCommandDto>()))
            .Returns(purchase);
        purchaseMock.Setup(p => p.UpdatePurchase(It.IsAny<Purchase>()).Result)
            .Returns(successResponse);
        
        var result = await sut.Handle(command, cancellationToken);
        
        mapperMock.Verify(m => m.Map<Purchase>(command.PurchaseDto));
        purchaseMock.Verify(p => p.UpdatePurchase(It.Is<Purchase>(
                pItem => pItem == purchase &&
                         pItem.Id == command.Id &&
                         pItem.Updated_At != null &&
                            pItem.Updated_At.Value.Date == DateTime.UtcNow.Date
                         )), Times.Once);
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        result.Id.Should().Be(command.Id); 
    }
    
}