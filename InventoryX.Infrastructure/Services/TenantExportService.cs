using System.IO.Compression;
using System.Text.Json;
using InventoryX.Application.Services.IServices;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services;

/// <summary>Creates a portable JSON archive of every Cycle 1 tenant-owned aggregate.</summary>
public sealed class TenantExportService(AppDbContext context, ITenantContext tenantContext) : ITenantExportService
{
    public async Task<byte[]> CreateArchiveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is required for export.");
        var payload = new
        {
            exportedAt = DateTime.UtcNow,
            tenant = await context.Tenants.AsNoTracking().SingleAsync(item => item.Id == tenantId, cancellationToken),
            subscriptions = await context.Subscriptions.AsNoTracking().ToListAsync(cancellationToken),
            products = await context.Products.AsNoTracking().ToListAsync(cancellationToken),
            categories = await context.Categories.AsNoTracking().ToListAsync(cancellationToken),
            locations = await context.Locations.AsNoTracking().ToListAsync(cancellationToken),
            stockLevels = await context.StockLevels.AsNoTracking().ToListAsync(cancellationToken),
            stockMovements = await context.StockMovements.AsNoTracking().ToListAsync(cancellationToken),
            sales = await context.Sales.AsNoTracking().ToListAsync(cancellationToken),
            saleLines = await context.SaleLines.AsNoTracking().ToListAsync(cancellationToken),
            salePayments = await context.SalePayments.AsNoTracking().ToListAsync(cancellationToken),
            receipts = await context.Receipts.AsNoTracking().ToListAsync(cancellationToken),
            registers = await context.Registers.AsNoTracking().ToListAsync(cancellationToken),
            shifts = await context.Shifts.AsNoTracking().ToListAsync(cancellationToken),
            invoices = await context.BillingInvoices.AsNoTracking().ToListAsync(cancellationToken),
        };
        await using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("inventoryx-export.json", CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await JsonSerializer.SerializeAsync(stream, payload, new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
        }
        return memory.ToArray();
    }
}
