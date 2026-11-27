using InventoryX.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryX.Infrastructure.BackgroundJobs;

/// <summary>Reconciles signed webhook deliveries after the ingress request has been acknowledged.</summary>
public sealed class PaystackInboxWorker(
    IServiceScopeFactory scopeFactory, ILogger<PaystackInboxWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<PaystackWebhookProcessor>()
                    .ReconcilePendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Paystack inbox reconciliation failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
