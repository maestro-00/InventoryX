using System.Text.Json;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services;

public sealed class ReceiptBuilder(IAppDbContext context, ITenantContext tenantContext) : IReceiptBuilder
{
    public async Task<Receipt> BuildAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException("Receipt generation requires a tenant context.");
        var tenant = await context.Tenants.SingleAsync(t => t.Id == tenantId, cancellationToken);
        var next = await context.Receipts.Select(r => (long?)r.SequenceNumber).MaxAsync(cancellationToken) + 1 ?? 1;
        var number = $"{DateTime.UtcNow:yyyy}-{next:D8}";
        var payload = JsonSerializer.Serialize(new
        {
            receiptNumber = number,
            issuedAt = DateTime.UtcNow,
            country = tenant.Country,
            currency = tenant.Currency,
            business = new { tenant.Name, tenant.Address, tenant.Phone },
            sale = new { sale.Id, sale.OccurredAt, sale.CashierId, sale.Subtotal, sale.DiscountTotal, sale.TaxTotal, sale.GrandTotal },
            lines = sale.Lines.Select(l => new { l.ProductName, l.Qty, l.UnitPrice, l.LineDiscount, taxComponents = JsonDocument.Parse(l.TaxComponents).RootElement.Clone(), l.TaxAmount, l.LineTotal }),
            payments = sale.Payments.Select(p => new { tender = p.Tender.ToString(), p.Amount, p.Reference }),
            fiscalFormat = $"{tenant.Country}-default",
        });
        var receipt = new Receipt { TenantId = tenantId, SaleId = sale.Id, SequenceNumber = next, Number = number, PayloadJson = payload };
        context.Receipts.Add(receipt);
        return receipt;
    }
}
