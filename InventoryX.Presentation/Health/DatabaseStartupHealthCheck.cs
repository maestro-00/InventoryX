using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InventoryX.Presentation.Health
{
    public sealed class DatabaseStartupHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(DatabaseStartupHealth.IsHealthy
                ? HealthCheckResult.Healthy("Database migrations and seed completed successfully.")
                : HealthCheckResult.Unhealthy("Database migrations or seed failed during startup."));
    }
}
