using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Commands.RequestHandlers.Sync;
using InventoryX.Application.Commands.Requests.Sync;
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

public sealed class OfflineSaleIngestTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid(), "cashier-1");

    [Fact]
    public async Task Same_client_sale_id_replays_original_without_duplicate_stock_effect()
    {
        await using var context = _db.CreateContext();
        var location = new Location { Name = "Main" };
        var product = new Product { Name = "Sugar", SellingPrice = 10m };
        var register = new Register { Name = "R1", LocationId = location.Id };
        var shift = new Shift { RegisterId = register.Id, OpenedBy = "cashier-1", OpenedAt = DateTime.UtcNow };
        context.AddRange(location, product, register, shift);
        await context.SaveChangesAsync();
        var ledger = new StockLedger(context);
        await ledger.AppendAsync([new StockMovementRequest(MovementType.Adjustment, product.Id, location.Id, 10m)]);
        await context.SaveChangesAsync();
        TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new CreateSaleCommandHandler(context, ledger, new TaxCalculator(), _db.TenantContext, Mock.Of<IPlanEnforcer>(), new PosAccess(context, _db.TenantContext));
        var clientSaleId = Guid.NewGuid();
        var command = new CreateSaleCommand
        {
            ClientSaleId = clientSaleId, RegisterId = register.Id, ShiftId = shift.Id,
            OfflineOrigin = true, OccurredAt = DateTime.UtcNow.AddMinutes(-5),
            Lines = [new CreateSaleLineDto { ProductId = product.Id, Qty = 2m }],
            Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 20m }],
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var replay = await handler.Handle(command, CancellationToken.None);

        replay.Id.Should().Be(first.Id);
        (await context.Sales.CountAsync()).Should().Be(1);
        (await context.StockLevels.SingleAsync()).QtyOnHand.Should().Be(8m);
        (await context.StockMovements.CountAsync(m => m.Type == MovementType.Sale)).Should().Be(1);
    }

    [Fact]
    public async Task Batch_returns_per_sale_results_and_honors_occurrence_time()
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
        var occurredAt = DateTime.UtcNow.AddHours(-2);
        var validId = Guid.NewGuid();
        TestPosAccess.Cashier(context, _db.TenantContext);
        var handler = new IngestOfflineSalesCommandHandler(
            context, ledger, new TaxCalculator(), _db.TenantContext, Mock.Of<IPlanEnforcer>(),
            new PosAccess(context, _db.TenantContext));

        var results = await handler.Handle(new IngestOfflineSalesCommand
        {
            Sales =
            [
                new CreateSaleCommand
                {
                    ClientSaleId = validId, RegisterId = register.Id, ShiftId = shift.Id, OccurredAt = occurredAt,
                    Lines =
                    [
                        new CreateSaleLineDto
                        {
                            ProductId = product.Id, Qty = 2m, UnitPrice = 10m,
                            TaxComponentsJson = """[{"code":"GET","amount":0.30},{"code":"NHIL","amount":0.50},{"code":"VAT","amount":1.49}]""",
                        },
                    ],
                    Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 22.29m }],
                },
                new CreateSaleCommand
                {
                    RegisterId = register.Id, ShiftId = shift.Id,
                    Lines =
                    [
                        new CreateSaleLineDto
                        {
                            ProductId = Guid.NewGuid(), Qty = 1m, UnitPrice = 10m,
                            TaxComponentsJson = "[]",
                        },
                    ],
                    Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 10m }],
                },
            ],
        }, CancellationToken.None);

        results.Should().ContainSingle(r => r.ClientSaleId == validId && r.Status == "applied_with_conflict");
        results.Should().ContainSingle(r => r.Status == "rejected" && r.Error != null);
        (await context.Sales.SingleAsync(s => s.ClientSaleId == validId)).OccurredAt.Should().Be(occurredAt);
        (await context.RejectedOfflineSales.CountAsync(r => r.Status == Domain.Models.Sync.RejectedOfflineSaleStatus.Open))
            .Should().Be(1);

        var replay = await handler.Handle(new IngestOfflineSalesCommand
        {
            Sales =
            [
                new CreateSaleCommand
                {
                    ClientSaleId = validId, RegisterId = register.Id, ShiftId = shift.Id, OccurredAt = occurredAt,
                    Lines =
                    [
                        new CreateSaleLineDto
                        {
                            ProductId = product.Id, Qty = 2m, UnitPrice = 10m,
                            TaxComponentsJson = """[{"code":"GET","amount":0.30},{"code":"NHIL","amount":0.50},{"code":"VAT","amount":1.49}]""",
                        },
                    ],
                    Payments = [new CreateSalePaymentDto { Tender = "Cash", Amount = 22.29m }],
                },
            ],
        }, CancellationToken.None);
        replay.Single().SaleId.Should().Be(results.Single(r => r.ClientSaleId == validId).SaleId);
        (await context.Sales.CountAsync()).Should().Be(1);
    }

    public void Dispose() => _db.Dispose();
}
