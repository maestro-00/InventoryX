using System.Text.Json;
using FluentAssertions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Application.Tests.Selling;

public sealed class ReceiptBuilderTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public ReceiptBuilderTests() => _db = new TestDb(_tenantId, "cashier-1");

    [Fact]
    public async Task Builds_sequential_tenant_receipts_with_structured_tax_payload()
    {
        await using var context = _db.CreateContext();
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Maestro Stores" });
        await context.SaveChangesAsync();
        var builder = new ReceiptBuilder(context, _db.TenantContext);

        var firstSale = SaleWithTax();
        context.Sales.Add(firstSale);
        var first = await builder.BuildAsync(firstSale);
        await context.SaveChangesAsync();

        var secondSale = SaleWithTax();
        context.Sales.Add(secondSale);
        var second = await builder.BuildAsync(secondSale);
        await context.SaveChangesAsync();

        first.SequenceNumber.Should().Be(1);
        second.SequenceNumber.Should().Be(2);
        second.Number.Should().EndWith("00000002");
        using var payload = JsonDocument.Parse(first.PayloadJson);
        payload.RootElement.GetProperty("lines")[0].GetProperty("taxComponents")[0]
            .GetProperty("name").GetString().Should().Be("VAT");
    }

    private static Sale SaleWithTax() => new()
    {
        LocationId = Guid.NewGuid(),
        RegisterId = Guid.NewGuid(),
        ShiftId = Guid.NewGuid(),
        CashierId = "cashier-1",
        ClientSaleId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        Subtotal = 10m,
        TaxTotal = 1.5m,
        GrandTotal = 11.5m,
        Lines =
        [
            new SaleLine
            {
                ProductId = Guid.NewGuid(), ProductName = "Sugar", Qty = 1m, UnitPrice = 10m,
                TaxComponents = "[{\"name\":\"VAT\",\"rate\":15,\"amount\":1.5}]", TaxAmount = 1.5m, LineTotal = 11.5m,
            },
        ],
        Payments = [new SalePayment { Tender = TenderType.Cash, Amount = 11.5m }],
    };

    public void Dispose() => _db.Dispose();
}
