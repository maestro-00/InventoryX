using System.Text;
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
            PdfContent = CreatePdf($"Invoice for {tenant.Name} | {amount:0.00} {tenant.Currency}"),
        };
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

    private static string CreatePdf(string text)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        return $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj\n4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n5 0 obj<</Length {escaped.Length + 35}>>stream\nBT /F1 14 Tf 72 720 Td ({escaped}) Tj ET\nendstream\nendobj\ntrailer<</Root 1 0 R>>\n%%EOF";
    }
}
