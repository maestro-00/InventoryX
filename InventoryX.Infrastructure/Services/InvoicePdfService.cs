using System.Text;
using System.Globalization;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services;

/// <summary>Creates a compact, self-contained PDF invoice and queues it to the billing contact.</summary>
public sealed class InvoicePdfService(AppDbContext context, IEmailSender emailSender) : IBillingInvoiceService
{
    public async Task<BillingInvoice> GenerateAndEmailAsync(Subscription subscription, string paymentReference, CancellationToken cancellationToken = default)
    {
        var existing = await context.BillingInvoices.SingleOrDefaultAsync(item => item.PaymentReference == paymentReference, cancellationToken);
        if (existing is not null) return existing;
        var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
        var plan = await context.PlanDefinitions.SingleAsync(item => item.Id == subscription.PlanDefinitionId, cancellationToken);
        var amount = subscription.BillingCycle == BillingCycle.Annual ? plan.AnnualPrice : plan.MonthlyPrice;
        var invoice = new BillingInvoice
        {
            TenantId = subscription.TenantId,
            SubscriptionId = subscription.Id,
            Number = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Amount = amount,
            Currency = tenant.Currency,
            PaymentReference = paymentReference,
            BillingContactSnapshot = tenant.BillingEmail,
            TaxDetailsSnapshot = tenant.BillingTaxNumber,
        };
        invoice.PdfContent = Convert.ToBase64String(RenderInvoice(invoice, tenant.Name, plan.Name));
        if (!string.IsNullOrWhiteSpace(tenant.BillingEmail))
        {
            await emailSender.SendEmailAsync(tenant.BillingEmail, $"Invoice {invoice.Number}",
                $"Your {plan.Name} subscription invoice is attached in your InventoryX billing history.");
            invoice.EmailedTo = tenant.BillingEmail;
            invoice.EmailedAt = DateTime.UtcNow;
        }
        context.BillingInvoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public static byte[] RenderInvoice(BillingInvoice invoice, string businessName, string planName)
    {
        var lines = new[]
        {
            "InventoryX paid invoice / receipt",
            $"Invoice: {invoice.Number}",
            $"Business: {businessName}",
            $"Plan: {planName}",
            $"Billing contact: {invoice.BillingContactSnapshot ?? "Not provided"}",
            $"Tax number: {invoice.TaxDetailsSnapshot ?? "Not provided"}",
            $"Period: {invoice.PeriodStart:yyyy-MM-dd} to {invoice.PeriodEnd:yyyy-MM-dd} UTC",
            $"Base: {(invoice.Amount - invoice.TaxAmount).ToString("0.00", CultureInfo.InvariantCulture)} {invoice.Currency}",
            $"Tax: {invoice.TaxAmount.ToString("0.00", CultureInfo.InvariantCulture)} {invoice.Currency}",
            $"Paid total: {invoice.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {invoice.Currency}",
            $"Payment reference: {invoice.PaymentReference ?? "Not available"}",
        };
        static string Escape(string value) => new string(value.Select(character =>
                character is >= ' ' and <= '~' ? character : '?').ToArray())
            .Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var content = "BT /F1 12 Tf 48 760 Td 18 TL\n" +
            string.Join("\n", lines.Select(line => $"({Escape(line)}) Tj T*")) + "\nET\n";
        var streamBytes = Encoding.ASCII.GetBytes(content);
        using var output = new MemoryStream();
        static void Write(MemoryStream stream, string value) =>
            stream.Write(Encoding.ASCII.GetBytes(value));
        Write(output, "%PDF-1.4\n");
        var offsets = new List<long>();
        void AddObject(int number, string value)
        {
            offsets.Add(output.Position);
            Write(output, $"{number} 0 obj\n{value}\nendobj\n");
        }
        AddObject(1, "<</Type/Catalog/Pages 2 0 R>>");
        AddObject(2, "<</Type/Pages/Count 1/Kids[3 0 R]>>");
        AddObject(3, "<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>");
        AddObject(4, "<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>");
        offsets.Add(output.Position);
        Write(output, $"5 0 obj\n<</Length {streamBytes.Length}>>\nstream\n");
        output.Write(streamBytes);
        Write(output, "endstream\nendobj\n");
        var xref = output.Position;
        Write(output, "xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<</Size 6/Root 1 0 R>>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }
}
