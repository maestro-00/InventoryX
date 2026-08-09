using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Services.IServices;

public interface IBillingInvoiceService
{
    Task<BillingInvoice> GenerateAndEmailAsync(Subscription subscription, string paymentReference, CancellationToken cancellationToken = default);
}
