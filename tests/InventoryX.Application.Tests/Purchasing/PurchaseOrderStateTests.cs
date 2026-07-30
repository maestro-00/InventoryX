using FluentAssertions;
namespace InventoryX.Application.Tests.Purchasing;
public sealed class PurchaseOrderStateTests
{
    [Fact]
    public void Purchase_order_state_machine_contract_exists()
    {
        Type.GetType("InventoryX.Domain.Models.Purchasing.PurchaseOrder, InventoryX.Domain").Should().NotBeNull("PO transitions and approval thresholds must be enforced");
        Type.GetType("InventoryX.Application.Commands.RequestHandlers.Purchasing.PurchaseOrderCommandHandler, InventoryX.Application").Should().NotBeNull();
    }
}
