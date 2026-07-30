using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Services.IServices
{
    public interface IReceiptDeliveryService
    {
        Task DeliverAsync(Receipt receipt, ReceiptChannel channel, string destination, CancellationToken cancellationToken = default);
    }
}