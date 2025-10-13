using InventoryX.Application.DTOs.Users;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure;
using InventoryX.Presentation.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using static InventoryX.Application.Extensions.IdentityApiEndpointRouteBuilderExtensions;


var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Azure App Service
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddInfrastructure(builder.Configuration).AddApplication().AddAuth().AddPresentation(builder.Configuration);

var app = builder.Build();

// Run database migrations on startup (Azure deployment)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
        // Don't throw - let app start so we can see detailed errors in Azure logs
    }
}

// Enable Swagger in all environments for Azure testing
app.UseSwagger();
app.UseSwaggerUI();

// Configure for Azure App Service
app.UseForwardedHeaders();
var group = app.MapGroup("/api/auth")
    .MapCustomIdentityApi<User>();

app.MapPost("/api/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});
app.MapGet("/api/auth/pingauth", (ClaimsPrincipal user) =>
{
    var email = user.FindFirstValue(ClaimTypes.Email);
    return Results.Json(new { Email = email });
}).RequireAuthorization();
app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");
app.UseAuthorization();
app.MapControllers();

app.Run();
