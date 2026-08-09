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
                .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                              ?? "Server=localhost;Database=InventoryX;TrustServerCertificate=True;Integrated Security=false;User Id=sa;Password=design-time-only")
                .Options;
            return new AppDbContext(options);
        }
    }
}
