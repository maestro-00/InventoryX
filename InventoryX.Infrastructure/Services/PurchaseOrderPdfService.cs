using System.Globalization;
using System.Text;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services;

public sealed class PurchaseOrderPdfService(IAppDbContext context, IAttachmentEmailSender emailSender) : IPurchaseOrderPdfService
{
    public async Task<PurchaseOrderDocument> GenerateAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(purchaseOrderId, cancellationToken);
        var number = order.Id.ToString("N")[..8].ToUpperInvariant();
        var lines = order.Lines.Select(line =>
            $"{line.Description} | Qty {line.OrderedQty.ToString("0.####", CultureInfo.InvariantCulture)} | Unit {line.UnitCost.ToString("0.00", CultureInfo.InvariantCulture)} | Total {(line.OrderedQty * line.UnitCost).ToString("0.00", CultureInfo.InvariantCulture)}");
        var text = string.Join("\n", new[]
        {
            $"PURCHASE ORDER PO-{number}",
            $"Supplier: {order.Supplier!.Name}",
            $"Address: {order.Supplier.Address ?? "-"}",
            $"Required by: {order.RequiredBy:yyyy-MM-dd}",
            $"Origin: {order.Origin}",
            "",
        }.Concat(lines).Concat(["", $"TOTAL: {order.Total.ToString("0.00", CultureInfo.InvariantCulture)}"]));
        return new PurchaseOrderDocument($"PO-{number}.pdf", "application/pdf", CreatePdf(text));
    }

    public async Task<PurchaseOrderEmailResult> EmailAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(purchaseOrderId, cancellationToken);
        if (order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.AwaitingApproval or PurchaseOrderStatus.Cancelled)
            throw new ConflictException("Only approved and sent purchase orders can be emailed.");
        if (string.IsNullOrWhiteSpace(order.Supplier!.Email))
            throw new FluentValidation.ValidationException("The supplier requires an email address before the purchase order can be sent.");
        var document = await GenerateAsync(purchaseOrderId, cancellationToken);
        await emailSender.SendAsync(order.Supplier.Email, $"Purchase order {document.FileName[..^4]}",
            $"<p>Please find purchase order <strong>{document.FileName[..^4]}</strong> attached.</p>",
            document.FileName, document.ContentType, document.Content, cancellationToken);
        return new PurchaseOrderEmailResult(order.Id, order.Supplier.Email, document.FileName);
    }

    private async Task<PurchaseOrder> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await context.PurchaseOrders.AsNoTracking().Include(order => order.Supplier).Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken)
        ?? throw new NotFoundException("Purchase order not found.");

    private static byte[] CreatePdf(string text)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "");
        var commands = new StringBuilder("BT /F1 10 Tf 50 750 Td 13 TL\n");
        foreach (var line in escaped.Split('\n')) commands.Append('(').Append(line).Append(") Tj T*\n");
        commands.Append("ET");
        var stream = commands.ToString();
        var pdf = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj\n4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n5 0 obj<</Length {Encoding.UTF8.GetByteCount(stream)}>>stream\n{stream}\nendstream\nendobj\ntrailer<</Root 1 0 R>>\n%%EOF";
        return Encoding.UTF8.GetBytes(pdf);
    }
}
