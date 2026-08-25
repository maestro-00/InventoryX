using System.Text;
using InventoryX.Application.Extensions;
using InventoryX.Application.Options;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Data.Seed;
using InventoryX.Presentation.Authentication;
using InventoryX.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using MediatR;
using InventoryX.Presentation.Swagger;
using System.Threading.RateLimiting;

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
                        builder.WithOrigins(allowedOrigins)
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    });
            });
            services.AddControllers();
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
                options.AddPolicy("webhook", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
            });
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LocationScopeAuthorizationHandler<,>));
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "InventoryX API",
                    Version = "v1",
                    Description =
                        "Cycle 1 multi-tenant inventory and POS API. Contracts live in " +
                        "specs/001-inventory-pos-platform/contracts/. Authenticate with " +
                        "Authorization: Bearer <JWT> unless an operation is anonymous.",
                });
                opt.AddServer(new OpenApiServer { Url = "/" });
                opt.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "JWT access token from POST /api/v1/auth/login or /auth/register.",
                });
                opt.OperationFilter<BearerSecurityRequirementsOperationFilter>();
                opt.OperationFilter<ControllerTagsOperationFilter>();
                opt.OperationFilter<LiveOnlyOperationFilter>();

                var presentationXml = Path.Combine(AppContext.BaseDirectory, "InventoryX.Presentation.xml");
                if (File.Exists(presentationXml))
                    opt.IncludeXmlComments(presentationXml, includeControllerXmlComments: true);
                var applicationXml = Path.Combine(AppContext.BaseDirectory, "InventoryX.Application.xml");
                if (File.Exists(applicationXml))
                    opt.IncludeXmlComments(applicationXml);
            });

           // This ensures Identity properly configures the authentication schemes
            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddApiEndpoints()
                .AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            });

            // Configure Identity's existing cookie instead of adding a new one
            services.ConfigureApplicationCookie(options =>
            {
                // Cookie configuration for cross-site auth
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                // Prevent redirects in API responses
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

            // JWT bearer for the versioned API surface (T018): tokens carry
            // tenant_id, role and location_scope claims.
            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
            var signingKey = jwtOptions.ResolveSigningKey();

            var authBuilder = services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                });

            // Add Google OAuth only when configured (owner sign-up/sign-in)
            var googleClientId = configuration["Authentication:Google:ClientId"];
            var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authBuilder.AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = googleClientId;
                    googleOptions.ClientSecret = googleClientSecret;
                    googleOptions.CallbackPath = "/api/auth/google-callback";
                    googleOptions.SaveTokens = true;
                    googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
                    googleOptions.Events.OnTicketReceived = GoogleOAuthHandler.OnTicketReceived;
                });
            }

            // Accept either the JWT bearer (API clients) or the Identity cookie
            services.AddHttpContextAccessor();
            services.AddSingleton<IAuthorizationHandler, Authorization.RegisterTokenAuthorizationHandler>();
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(
                        JwtBearerDefaults.AuthenticationScheme,
                        IdentityConstants.ApplicationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("RegisterTokenOrUser", policy =>
                {
                    policy.AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme,
                        IdentityConstants.ApplicationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new Authorization.RegisterTokenRequirement());
                });
            });

            return services;
        }

        public static WebApplication UsePresentation(this WebApplication app)
        {
            app.UseMiddleware<ProblemDetailsMiddleware>();

            app.UseSerilogRequestLogging(options =>
                options.EnrichDiagnosticContext = RequestLogEnricher.Enrich);

            // Configure CORS - must come before other middleware
            app.UseCors("AllowSpecificOrigin");

            // Forward headers for proxies (important for Azure)
            app.UseForwardedHeaders();

            // Run database migrations on startup (Azure deployment). Skip in Testing —
            // WebApplicationFactory tests create schema via EnsureCreated on a shared Sqlite.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    dbContext.Database.Migrate();
                    DataSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
                    app.Logger.LogInformation("Database migrations and seeds applied successfully");
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

            // app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Response.Headers.XFrameOptions = "DENY";
                context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                await next();
            });

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseMiddleware<TenantResolutionMiddleware>();
            app.UseAuthorization();

            app.MapControllers();

            app.MapGroup("/api/auth")
                .RequireRateLimiting("auth")
                .ExcludeFromDescription()
                .MapCustomIdentityApi<User>();

            return app;
        }
    }
}
