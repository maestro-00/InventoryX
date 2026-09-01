using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryX.Infrastructure.Data
{
    /// <summary>Design-time factory for `dotnet ef` commands (no tenant context, no live connection needed).</summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                           ?? "Host=localhost;Port=5432;Database=InventoryX;Username=postgres;Password=postgres")
                .Options;
            return new AppDbContext(options);
        }
    }
}
