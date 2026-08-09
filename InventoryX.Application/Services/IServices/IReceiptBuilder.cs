using InventoryX.Domain.Models.Selling;

namespace InventoryX.Application.Services.IServices;

public interface IReceiptBuilder
{
    Task<Receipt> BuildAsync(Sale sale, CancellationToken cancellationToken = default);
}
