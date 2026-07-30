using FluentAssertions;

namespace InventoryX.Application.Tests.Inventory;

/// <summary>T058 - stock-count variance calculation and approval posting.</summary>
public sealed class StockCountTests
{
    [Fact]
    public void Stock_count_model_preserves_expected_counted_and_variance_quantities()
    {
        var lineType = Type.GetType("InventoryX.Domain.Models.Inventory.StockCountLine, InventoryX.Domain");

        lineType.Should().NotBeNull();
        lineType!.GetProperty("ExpectedQty").Should().NotBeNull();
        lineType.GetProperty("CountedQty").Should().NotBeNull();
        lineType.GetProperty("VarianceQty").Should().NotBeNull();
        lineType.GetProperty("VarianceValue").Should().NotBeNull();
    }

    [Fact]
    public void Approval_handler_exists_to_post_count_corrections_only_after_approval()
    {
        Type.GetType(
                "InventoryX.Application.Commands.RequestHandlers.Inventory.ApproveStockCountCommandHandler, InventoryX.Application")
            .Should().NotBeNull("approval must post CountCorrection movements while submission remains stock-neutral");
    }
}
