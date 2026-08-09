using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Inventory;

/// <summary>T058 - stock-count variance calculation and approval posting.</summary>
public sealed class StockCountTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "counter-1");

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

    [Fact]
    public async Task Submitted_variance_is_stock_neutral_until_approval_posts_correction()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 4m };
        context.AddRange(location, product);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m, UnitCost: 2m)]);
        await context.SaveChangesAsync();

        var opened = await new OpenStockCountCommandHandler(context, _db.TenantContext).Handle(
            new OpenStockCountCommand { LocationId = location.Id, Scope = "Full" }, CancellationToken.None);
        var line = opened.Lines.Single();
        await new UpdateStockCountLinesCommandHandler(context).Handle(new UpdateStockCountLinesCommand
        {
            CountId = opened.Id,
            Lines = [new StockCountLineInput(line.Id, 7m)],
        }, CancellationToken.None);
        var submitted = await new SubmitStockCountCommandHandler(context).Handle(
            new SubmitStockCountCommand { CountId = opened.Id }, CancellationToken.None);

        submitted.Lines.Single().VarianceQty.Should().Be(-3m);
        submitted.Lines.Single().VarianceValue.Should().Be(-6m);
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(10m);

        _db.TenantContext.UserId = "manager-1";
        await new ApproveStockCountCommandHandler(context, ledger, _db.TenantContext).Handle(
            new ApproveStockCountCommand { CountId = opened.Id }, CancellationToken.None);
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(7m);
        (await context.StockMovements.OrderByDescending(m => m.OccurredAt).FirstAsync()).Type
            .Should().Be(MovementType.CountCorrection);
    }

    public void Dispose() => _db.Dispose();
}
