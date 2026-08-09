using System.Text;
using FluentAssertions;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Purchasing;
using InventoryX.Infrastructure.Services;

namespace InventoryX.Infrastructure.Tests.Services;

public sealed class PurchaseOrderPdfServiceTests : IDisposable
{
    private readonly TestDb _db = new(Guid.NewGuid());

    [Fact]
    public async Task Generate_creates_pdf_with_supplier_order_and_line_details()
    {
        await using var context = _db.CreateContext();
        var order = await SeedSentOrderAsync(context);
        var mailer = new RecordingAttachmentSender();

        var document = await new PurchaseOrderPdfService(context, mailer).GenerateAsync(order.Id);

        Encoding.UTF8.GetString(document.Content).Should().StartWith("%PDF-1.4");
        Encoding.UTF8.GetString(document.Content).Should().Contain("Acme Supplies").And.Contain("Sugar");
        document.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Email_attaches_generated_pdf_to_supplier_address()
    {
        await using var context = _db.CreateContext();
        var order = await SeedSentOrderAsync(context);
        var mailer = new RecordingAttachmentSender();

        var result = await new PurchaseOrderPdfService(context, mailer).EmailAsync(order.Id);

        result.EmailedTo.Should().Be("orders@acme.test");
        mailer.To.Should().Be("orders@acme.test");
        mailer.ContentType.Should().Be("application/pdf");
        mailer.Attachment.Should().StartWith(Encoding.UTF8.GetBytes("%PDF-1.4"));
    }

    private static async Task<PurchaseOrder> SeedSentOrderAsync(Infrastructure.Data.AppDbContext context)
    {
        var supplier = new Supplier { Name = "Acme Supplies", Email = "orders@acme.test", Address = "Accra" };
        var order = new PurchaseOrder
        {
            Supplier = supplier, SupplierId = supplier.Id,
            Lines = [new PurchaseOrderLine { ProductId = Guid.NewGuid(), Description = "Sugar", OrderedQty = 5m, UnitCost = 12m }],
        };
        order.Submit(false, DateTime.UtcNow);
        context.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    public void Dispose() => _db.Dispose();

    private sealed class RecordingAttachmentSender : IAttachmentEmailSender
    {
        public string? To { get; private set; }
        public byte[] Attachment { get; private set; } = [];
        public string? ContentType { get; private set; }
        public Task SendAsync(string to, string subject, string htmlBody, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
        { To = to; Attachment = content; ContentType = contentType; return Task.CompletedTask; }
    }
}
