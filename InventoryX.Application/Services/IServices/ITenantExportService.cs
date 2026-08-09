namespace InventoryX.Application.Services.IServices;

public interface ITenantExportService
{
    Task<byte[]> CreateArchiveAsync(CancellationToken cancellationToken = default);
}
