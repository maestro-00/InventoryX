using InventoryX.Domain.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Data.Seed;

public static class AdjustmentReasonSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        foreach (var code in new[] { "Damage", "Theft", "Spoilage", "Expiry", "Sample", "PersonalUse", "Correction" })
        {
            if (!await context.AdjustmentReasons.AnyAsync(r => r.TenantId == null && r.Code == code, cancellationToken))
                context.AdjustmentReasons.Add(new AdjustmentReason { Code = code, Name = code, IsSystem = true });
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
