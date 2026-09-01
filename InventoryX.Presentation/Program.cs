using InventoryX.Infrastructure;
using InventoryX.Presentation.Configuration;
using InventoryX.Application;
using InventoryX.Application.Options;
using InventoryX.Presentation.Health;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsProduction())
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (jwtOptions.UsesDevelopmentSigningKey())
            throw new InvalidOperationException(
                "Jwt:SigningKey must be set to a strong secret in Production (not empty or a placeholder).");
    }

    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
        .AddCheck<DatabaseStartupHealthCheck>("database_startup");

    builder.Services.AddInfrastructure(builder.Configuration).AddApplication(builder.Configuration).AddPresentation(builder.Configuration);

    var app = builder.Build();

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false,
    });
    app.MapHealthChecks("/health/ready");

    app.UsePresentation();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
