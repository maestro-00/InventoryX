using InventoryX.Infrastructure;
using InventoryX.Presentation.Configuration;
using InventoryX.Application;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.AddHealthChecks();

    builder.Services.AddInfrastructure(builder.Configuration).AddApplication(builder.Configuration).AddPresentation(builder.Configuration);

    var app = builder.Build();

    app.MapHealthChecks("/health/live");
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
