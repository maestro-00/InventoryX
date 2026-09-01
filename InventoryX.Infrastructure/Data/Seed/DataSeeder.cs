using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using InventoryX.Domain.Models;

namespace InventoryX.Infrastructure.Data.Seed
{
    /// <summary>Runs all global seeds (roles, plans, tax treatments) at startup after migrations.</summary>
    public static class DataSeeder
    {
        public static async Task SeedAsync(
            AppDbContext context,
            IConfiguration? configuration = null,
            UserManager<User>? userManager = null,
            CancellationToken cancellationToken = default)
        {
            await RoleSeeder.SeedAsync(context, cancellationToken);
            await PlanSeeder.SeedAsync(context, cancellationToken);
            await TaxSeeder.SeedAsync(context, cancellationToken);
            await AdjustmentReasonSeeder.SeedAsync(context, cancellationToken);

            var demoMode = configuration?["DEMO_MODE"];
            if (string.Equals(demoMode, "true", StringComparison.OrdinalIgnoreCase) && userManager is not null)
                await DemoSeeder.SeedAsync(context, userManager, cancellationToken);
        }
    }
}
