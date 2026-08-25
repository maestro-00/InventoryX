using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Sync;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Commands.Requests.Sync;
using InventoryX.Application.Queries.RequestHandlers.Sync;
using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Sync;
using InventoryX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InventoryX.Application.Tests.Sync;

public sealed class OfflineFiscalSnapshotTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Offline_ingest_accepts_historical_price_and_tax_without_recalculation()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        // Live catalogue price changed after the offline sale occurred.
        var product = new Product { Name = "Sugar", SellingPrice = 99m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow };
        context.AddRange(location, product, register, shift);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m)]);
        await context.SaveChangesAsync();

        TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new IngestOfflineSalesCommandHandler(
            context, ledger, new TaxCalculator(), _db.TenantContext, Mock.Of<IPlanEnforcer>(),
            new PosAccess(context, _db.TenantContext));
        var clientSaleId = Guid.NewGuid();
        var results = await handler.Handle(new IngestOfflineSalesCommand
        {
            Sales =
            [
                new CreateSaleCommand
                {
                    ClientSaleId = clientSaleId,
                    RegisterId = register.Id,
                    ShiftId = shift.Id,
                    OccurredAt = DateTime.UtcNow.AddHours(-1),
                    Lines =
                    [
                        new CreateSaleLineDto
                        {
                            ProductId = product.Id,
                            Qty = 1m,
                            UnitPrice = 10m,
                            TaxComponentsJson = """[{"code":"VAT","amount":1.49}]""",
                        },
                    ],
                    Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 11.49m }],
                },
            ],
        }, CancellationToken.None);

        results.Should().ContainSingle(r => r.Status == "applied");
        var sale = await context.Sales.Include(s => s.Lines).SingleAsync(s => s.ClientSaleId == clientSaleId);
        sale.Lines.Single().UnitPrice.Should().Be(10m);
        sale.Lines.Single().TaxAmount.Should().Be(1.49m);
        sale.GrandTotal.Should().Be(11.49m);
    }

    public void Dispose() => _db.Dispose();
}

public sealed class SyncSnapshotCompletenessTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Snapshot_includes_favourites_receipt_template_tracking_and_tombstones()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var product = new Product
        {
            Name = "Sugar",
            SellingPrice = 10m,
            AllowFractional = true,
            TrackingMode = TrackingMode.Batch,
        };
        var deleted = new Product { Name = "Gone", SellingPrice = 1m, IsDeleted = true };
        var favourites = new FavouritesLayout { RegisterId = register.Id, LayoutJson = """{"pages":[]}""" };
        context.AddRange(location, register, product, deleted, favourites);
        if (context.Tenants.Local.Count == 0 && !await context.Tenants.AnyAsync())
        {
            context.Tenants.Add(new Domain.Models.Tenancy.Tenant
            {
                Id = _db.TenantContext.TenantId!.Value,
                Name = "Test",
                ReceiptTemplate = """{"footer":"Thank you"}""",
            });
        }
        else
        {
            var tenant = await context.Tenants.FirstAsync();
            tenant.ReceiptTemplate = """{"footer":"Thank you"}""";
        }
        await context.SaveChangesAsync();

        var handler = new GetSyncSnapshotQueryHandler(context, _db.TenantContext);
        var snapshot = await handler.Handle(new GetSyncSnapshotQuery(register.Id), CancellationToken.None);

        snapshot.BundleVersion.Should().NotBeNullOrWhiteSpace();
        snapshot.Favourites.Should().NotBeNull();
        snapshot.Favourites!.LayoutJson.Should().Contain("pages");
        snapshot.ReceiptTemplate.Should().NotBeNull();
        snapshot.Products.Should().ContainSingle(p => p.Id == product.Id && p.AllowFractional && p.TrackingMode == "Batch");
        snapshot.Deleted.Should().ContainSingle(d => d.EntityType == "product" && d.Id == deleted.Id);
    }

    public void Dispose() => _db.Dispose();
}

public sealed class RejectedSaleReconciliationTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "manager-1");

    [Fact]
    public async Task Rejected_sale_can_be_released_for_retry_or_linked_to_reconciliation()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var register = new Register { Name = "R1", LocationId = location.Id };
        context.AddRange(location, register);
        var rejected = new RejectedOfflineSale
        {
            ClientSaleId = Guid.NewGuid(),
            RegisterId = register.Id,
            PayloadJson = "{}",
            PayloadHash = "abc",
            RejectionReason = "product missing",
            Status = RejectedOfflineSaleStatus.Open,
        };
        context.RejectedOfflineSales.Add(rejected);
        await context.SaveChangesAsync();

        var resolve = new ResolveRejectedOfflineSaleCommandHandler(context, _db.TenantContext);
        var released = await resolve.Handle(new ResolveRejectedOfflineSaleCommand
        {
            RejectedSaleId = rejected.Id,
            Resolution = "retryRelease",
            Note = "Catalogue restored",
        }, CancellationToken.None);
        released.Status.Should().Be(nameof(RejectedOfflineSaleStatus.ReleasedForRetry));

        var linkedSaleId = Guid.NewGuid();
        var second = new RejectedOfflineSale
        {
            ClientSaleId = Guid.NewGuid(),
            RegisterId = register.Id,
            PayloadJson = "{}",
            PayloadHash = "def",
            RejectionReason = "shift closed",
            Status = RejectedOfflineSaleStatus.Open,
        };
        context.RejectedOfflineSales.Add(second);
        var sale = new Sale
        {
            Id = linkedSaleId,
            LocationId = location.Id,
            RegisterId = register.Id,
            ShiftId = Guid.NewGuid(),
            CashierId = "manager-1",
            ClientSaleId = Guid.NewGuid(),
            Status = SaleStatus.Completed,
            OccurredAt = DateTime.UtcNow,
        };
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var reconciled = await resolve.Handle(new ResolveRejectedOfflineSaleCommand
        {
            RejectedSaleId = second.Id,
            Resolution = "reconcileLinked",
            LinkedReconciliationSaleId = linkedSaleId,
            Note = "Manual compensating sale",
        }, CancellationToken.None);
        reconciled.Status.Should().Be(nameof(RejectedOfflineSaleStatus.Reconciled));
        reconciled.LinkedReconciliationSaleId.Should().Be(linkedSaleId);
    }

    public void Dispose() => _db.Dispose();
}
