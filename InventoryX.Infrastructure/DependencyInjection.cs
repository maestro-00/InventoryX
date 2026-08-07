using InventoryX.Application.Options;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using InventoryX.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PaystackOptions>(configuration.GetSection(PaystackOptions.SectionName));
        services.AddHttpClient<IPaymentGateway, PaystackGateway>((provider, client) =>
        {
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaystackOptions>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
        });
        services.AddScoped<PaystackWebhookProcessor>();
        services.AddScoped<IBillingInvoiceService, InvoicePdfService>();
        services.AddScoped<IPurchaseOrderPdfService, PurchaseOrderPdfService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<ITenantExportService, TenantExportService>();
        services.AddHostedService<BillingWorker>();
        services.AddHostedService<AlertScanWorker>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IPlanEnforcer, PlanEnforcer>();
        services.AddScoped<IStockLedger, StockLedger>();
        services.AddScoped<IReceiptBuilder, ReceiptBuilder>();
        services.AddScoped<IReceiptDeliveryService, ReceiptDeliveryService>();
        services.AddScoped<ISpreadsheetImportService, SpreadsheetImportService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
