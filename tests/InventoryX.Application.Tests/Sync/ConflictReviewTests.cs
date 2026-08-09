using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Sync;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Commands.Requests.Sync;
using InventoryX.Application.Queries.RequestHandlers.Sync;
using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryX.Application.Tests.Sync;

public sealed class ConflictReviewTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Conflict_can_be_listed_and_resolved_with_a_compensating_adjustment()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow };
        context.AddRange(location, product, register, shift);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 1m)]);
        await context.SaveChangesAsync();
        var notifications = new NotificationService(context);
        var ingest = new IngestOfflineSalesCommandHandler(
            context, ledger, new TaxCalculator(), _db.TenantContext, Mock.Of<IPlanEnforcer>(), null, notifications);
        var ingested = await ingest.Handle(new IngestOfflineSalesCommand
        {
            Sales = [new InventoryX.Application.Commands.Requests.Selling.CreateSaleCommand
            {
                RegisterId = register.Id, ShiftId = shift.Id,
                Lines = [new InventoryX.Application.Commands.Requests.Selling.CreateSaleLineDto { ProductId = product.Id, Qty = 2m }],
                Payments = [new InventoryX.Application.Commands.Requests.Selling.CreateSalePaymentDto { Tender = "Cash", Amount = 20m }],
            }],
        }, CancellationToken.None);
        var saleId = ingested.Single().SaleId!.Value;

        (await new GetSyncConflictsQueryHandler(context).Handle(new GetSyncConflictsQuery(), CancellationToken.None))
            .Should().ContainSingle(s => s.Id == saleId);
        (await context.Notifications.SingleAsync()).ResolvedAt.Should().BeNull();

        await new ResolveSyncConflictCommandHandler(context, ledger, notifications).Handle(new ResolveSyncConflictCommand
        {
            SaleId = saleId,
            Resolution = "adjustWithReason",
            ReasonCode = "Recount",
            Adjustments = [new AdjustmentLineDto { ProductId = product.Id, QtyDelta = 1m }],
        }, CancellationToken.None);

        (await context.Sales.FindAsync(saleId))!.StockConflictFlag.Should().BeFalse();
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(0m);
        (await context.Notifications.SingleAsync()).ResolvedAt.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
