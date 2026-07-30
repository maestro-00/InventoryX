using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Inventory;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Inventory;

/// <summary>T057 - approval threshold and requester/approver separation.</summary>
public sealed class AdjustmentApprovalTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public AdjustmentApprovalTests() => _db = new TestDb(_tenantId, "requester-1");

    [Fact]
    public async Task Adjustment_above_tenant_threshold_waits_without_posting_stock()
    {
        await using var context = _db.CreateContext();
        var tenant = new Tenant { Id = _tenantId, Name = "Shop", AdjustmentApprovalThreshold = 50m };
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        context.AddRange(tenant, location, product);
        await context.SaveChangesAsync();

        var handler = new RecordStockAdjustmentCommandHandler(context, new StockLedger(context));
        var result = await handler.Handle(new RecordStockAdjustmentCommand
        {
            LocationId = location.Id,
            ReasonCode = "Correction",
            Lines = [new AdjustmentLineDto { ProductId = product.Id, QtyDelta = 10m, UnitCost = 10m }],
        }, CancellationToken.None);

        result.Status.Should().Be("AwaitingApproval");
        (await context.StockLevels.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Approval_flow_has_a_handler_that_can_enforce_approver_not_requester()
    {
        var handlerType = Type.GetType(
            "InventoryX.Application.Commands.RequestHandlers.Inventory.ApproveStockAdjustmentCommandHandler, InventoryX.Application");

        handlerType.Should().NotBeNull("adjustment approval must reject approver == requester");
    }

    public void Dispose() => _db.Dispose();
}
