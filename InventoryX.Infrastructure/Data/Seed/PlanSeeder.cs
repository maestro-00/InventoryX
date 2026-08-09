using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Data.Seed
{
    /// <summary>Seeds the four plan definitions with spec FR-009/FR-010 caps (T023).</summary>
    public static class PlanSeeder
    {
        public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            var plans = new List<PlanDefinition>
            {
                new()
                {
                    Tier = PlanTier.Free, Name = "Free",
                    MonthlyPrice = 0, AnnualPrice = 0,
                    MaxLocations = 1, MaxUsers = 2, MaxProducts = 100, MaxRegisters = 1,
                    MonthlySaleCap = 300, HistoryMonths = 3,
                },
                new()
                {
                    Tier = PlanTier.Standard, Name = "Standard",
                    MonthlyPrice = 199, AnnualPrice = 1990,
                    MaxLocations = 3, MaxUsers = 10, MaxProducts = 5000, MaxRegisters = 3,
                    MonthlySaleCap = 3000, HistoryMonths = 24,
                    PurchaseOrders = true,
                },
                new()
                {
                    Tier = PlanTier.Professional, Name = "Professional",
                    MonthlyPrice = 499, AnnualPrice = 4990,
                    MaxLocations = 10, MaxUsers = 30, MaxProducts = null, MaxRegisters = 10,
                    MonthlySaleCap = null, HistoryMonths = null,
                    PurchaseOrders = true, BatchExpiry = true, AdvancedReports = true,
                },
                new()
                {
                    Tier = PlanTier.Enterprise, Name = "Enterprise",
                    MonthlyPrice = 1299, AnnualPrice = 12990,
                    MaxLocations = null, MaxUsers = null, MaxProducts = null, MaxRegisters = null,
                    MonthlySaleCap = null, HistoryMonths = null,
                    PurchaseOrders = true, BatchExpiry = true, Serials = true, MultiCurrency = true,
                    CustomRoles = true, AdvancedReports = true, Integrations = true,
                },
            };

            foreach (var plan in plans)
            {
                if (!await context.PlanDefinitions.AnyAsync(p => p.Tier == plan.Tier, cancellationToken))
                    context.PlanDefinitions.Add(plan);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
