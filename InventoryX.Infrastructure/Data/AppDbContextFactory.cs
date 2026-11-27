using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace InventoryX.Infrastructure.Data
{
    /// <summary>Design-time factory using the API's configuration sources.</summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var presentation = Assembly.Load("InventoryX.Presentation");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                              ?? "Development";
            var basePath = Path.GetDirectoryName(presentation.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddUserSecrets(presentation, optional: true)
                .AddEnvironmentVariables()
                .Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("===", StringComparison.Ordinal))
                throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection in user secrets or ConnectionStrings__DefaultConnection in the environment.");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new AppDbContext(options);
        }
    }
}
