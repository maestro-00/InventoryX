using System.Security.Claims;
using InventoryX.Application.Extensions;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure;
using InventoryX.Presentation.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace InventoryX.Presentation.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {

            // Configure forwarded headers for Azure App Service
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
            builder =>
            {
                var allowedOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                builder.WithOrigins(allowedOrigins) // Ensure the correct port number
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials(); // Include credentials if needed
            });
            });
            services.AddControllers();
            //Add if only there is a cyclical reference
            //    .AddJsonOptions(options =>
            //{
            //    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            //});
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(opt =>
            {
                opt.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorisation",
                    Type = SecuritySchemeType.ApiKey
                });
                opt.OperationFilter<SecurityRequirementsOperationFilter>();
            }
            );

           // This ensures Identity properly configures the authentication schemes
            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddApiEndpoints()
                .AddDefaultTokenProviders();

            // Now configure authentication with Google OAuth
            services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
                    googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured");
                    googleOptions.CallbackPath = "/api/auth/google-callback";
                    googleOptions.SaveTokens = true;
                    googleOptions.SignInScheme = IdentityConstants.ExternalScheme;

                    googleOptions.Events.OnTicketReceived = GoogleOAuthHandler.OnTicketReceived;
                });

            //Configuring cookie auth handler for SPAs
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
            });

            return services;
        }

        public static WebApplication UsePresentation(this WebApplication app)
        {
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

            app.UseCors("AllowSpecificOrigin");

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapGroup("/api/auth")
                .MapCustomIdentityApi<User>();

            return app;
        }
    }
}
