using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Tests.Selling;

public sealed class ReceiptDeliveryCommandTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public ReceiptDeliveryCommandTests() => _db = new TestDb(_tenantId, "cashier-1");

    [Fact]
    public async Task Delivers_email_and_persists_a_successful_delivery_log()
    {
        await using var context = _db.CreateContext();
        var saleId = Guid.NewGuid();
        context.Sales.Add(new Sale
        {
            Id = saleId,
            TenantId = _tenantId,
            LocationId = Guid.NewGuid(),
            RegisterId = Guid.NewGuid(),
            ShiftId = Guid.NewGuid(),
            CashierId = "cashier-1",
            ClientSaleId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
        });
        context.Receipts.Add(new Receipt
        {
            TenantId = _tenantId,
            SaleId = saleId,
            SequenceNumber = 1,
            Number = "2026-00000001",
            PayloadJson = "{}",
        });
        await context.SaveChangesAsync();
        var delivery = new RecordingDeliveryService();
        var handler = new DeliverReceiptCommandHandler(context, delivery);

        var result = await handler.Handle(new DeliverReceiptCommand
        {
            SaleId = saleId,
            Channel = "email",
            Destination = "customer@example.com",
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        delivery.Calls.Should().Be(1);
        context.ReceiptDeliveryLogs.Should().ContainSingle(log =>
            log.Channel == ReceiptChannel.Email && log.Success && log.Destination == "customer@example.com");
    }

    public void Dispose() => _db.Dispose();

    private sealed class RecordingDeliveryService : IReceiptDeliveryService
    {
        public int Calls { get; private set; }

        public Task DeliverAsync(Receipt receipt, ReceiptChannel channel, string destination, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
