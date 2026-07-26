namespace InventoryX.Infrastructure.Data.Seed
{
    /// <summary>Runs all global seeds (roles, plans, tax treatments) at startup after migrations.</summary>
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            await RoleSeeder.SeedAsync(context, cancellationToken);
            await PlanSeeder.SeedAsync(context, cancellationToken);
            await TaxSeeder.SeedAsync(context, cancellationToken);
        }
    }
}
