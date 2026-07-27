using System.Reflection;
using FluentValidation;
using InventoryX.Application.Behaviors;
using InventoryX.Application.Options;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg => { }, assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline order: validation → plan enforcement → audit → handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PlanEnforcementBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITaxCalculator, TaxCalculator>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.Configure<AuthMessageSenderOptions>(configuration);
        services.Configure<AuthOptions>(configuration.GetSection("Frontend"));
        services.AddHttpContextAccessor();
        return services;
    }
}
