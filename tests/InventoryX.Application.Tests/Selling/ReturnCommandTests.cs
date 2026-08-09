using System.Reflection;
using FluentAssertions;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Selling;

/// <summary>
/// T046 - return contracts must preserve original commercial terms, represent
/// approval gates, and distinguish sellable from quarantined stock.
/// </summary>
public sealed class ReturnCommandTests
{
    private static readonly Assembly SellingAssembly = typeof(Sale).Assembly;

    [Fact]
    public void Return_transaction_tracks_original_sale_and_refund()
    {
        var type = SellingAssembly.GetType("InventoryX.Domain.Models.Selling.ReturnTransaction");

        type.Should().NotBeNull("returns must be first-class immutable commercial transactions");
        type!.GetProperty("OriginalSaleId").Should().NotBeNull();
        type.GetProperty("RefundTotal").Should().NotBeNull();
        type.GetProperty("RefundTender").Should().NotBeNull();
    }

    [Fact]
    public void Return_lines_snapshot_original_price_tax_and_disposition()
    {
        var type = SellingAssembly.GetType("InventoryX.Domain.Models.Selling.ReturnLine");

        type.Should().NotBeNull();
        type!.GetProperty("OriginalUnitPrice").Should().NotBeNull();
        type.GetProperty("OriginalTaxAmount").Should().NotBeNull();
        type.GetProperty("Disposition").Should().NotBeNull();
    }

    [Fact]
    public void Return_contract_carries_authorization_state()
    {
        var type = SellingAssembly.GetType("InventoryX.Domain.Models.Selling.ReturnTransaction");

        type.Should().NotBeNull();
        type!.GetProperty("AuthorizationRequired").Should().NotBeNull();
        type.GetProperty("AuthorizedBy").Should().NotBeNull();
        type.GetProperty("Status").Should().NotBeNull();
    }

    [Fact]
    public void Return_disposition_supports_to_stock_and_quarantine()
    {
        var type = SellingAssembly.GetType("InventoryX.Domain.Models.Selling.ReturnDisposition");

        type.Should().NotBeNull();
        Enum.GetNames(type!).Should().Contain(["ToStock", "Quarantine"]);
    }
}
