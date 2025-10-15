using System.Reflection;
using InventoryX.Application.Options;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg => {} , Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
        services.AddScoped<IInventoryItemService, InventoryItemService>();
        services.AddScoped<IInventoryItemTypeService, InventoryItemTypeService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IRetailStockService, RetailStockService>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.Configure<AuthMessageSenderOptions>(configuration);
        services.Configure<AuthOptions>(configuration);
        services.AddHttpContextAccessor();
        return services;
    }
}
