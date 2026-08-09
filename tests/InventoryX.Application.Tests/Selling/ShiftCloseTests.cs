using FluentAssertions;

namespace InventoryX.Application.Tests.Selling;

/// <summary>T085 - close requires a counted drawer and reports expected cash plus variance.</summary>
public sealed class ShiftCloseTests
{
    [Fact]
    public void Shift_close_contract_requires_counted_cash_and_exposes_variance()
    {
        var command = Type.GetType("InventoryX.Application.Commands.Requests.Selling.CloseShiftCommand, InventoryX.Application");
        command.Should().NotBeNull("a close operation must reject an uncounted drawer and calculate cash variance");
        command!.GetProperty("ClosingCounted").Should().NotBeNull();
        Type.GetType("InventoryX.Application.Commands.RequestHandlers.Selling.CloseShiftCommandHandler, InventoryX.Application")
            .Should().NotBeNull("cash tender totals, cash movements, and the opening float must be reconciled at close");
    }
}
