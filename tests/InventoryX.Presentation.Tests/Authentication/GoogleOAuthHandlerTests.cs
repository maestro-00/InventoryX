using System.Security.Claims;
using FluentAssertions;
using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Infrastructure.Services;
using InventoryX.Presentation.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryX.Presentation.Tests.Authentication;

public sealed class GoogleOAuthHandlerTests
{
    private static (IServiceProvider ServiceProvider, AppDbContext DbContext, SqliteConnection Connection) CreateServices()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connection));

        services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwtOptions = new JwtOptions
        {
            Issuer = "InventoryX.Test",
            Audience = "InventoryX.Client",
            SigningKey = "SuperSecretTestSigningKey1234567890!@#$%",
            AccessTokenMinutes = 60,
            RefreshTokenDays = 7,
        };
        services.AddSingleton(Options.Create(jwtOptions));

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        services.AddSingleton<IMemoryCache>(memoryCache);
        services.AddSingleton<ITokenService, JwtTokenService>();

        var configValues = new Dictionary<string, string?>
        {
            ["Frontend:AllowedOrigins:0"] = "http://localhost:5173",
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddHttpContextAccessor();

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Seed default owner role and professional plan
        db.AppRoles.Add(new Role { Id = Guid.NewGuid(), Name = "Owner" });
        db.PlanDefinitions.Add(new PlanDefinition
        {
            Id = Guid.NewGuid(),
            Tier = PlanTier.Professional,
            Name = "Professional Plan",
            IsActive = true,
        });
        db.SaveChanges();

        return (sp, db, connection);
    }

    [Fact]
    public async Task OnTicketReceived_Creates_New_User_And_Tenant_With_Client_Details()
    {
        var (sp, db, connection) = CreateServices();
        using var _ = connection;
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        sp.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var identity = new ClaimsIdentity("Google");
        identity.AddClaim(new Claim(ClaimTypes.Email, "newoauthuser@example.com"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "google-sub-12345"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Jane OAuth"));
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties();
        authProperties.Items["returnUrl"] = "http://localhost:5173/dashboard";
        authProperties.Items["businessName"] = "Jane's Bakery";
        authProperties.Items["country"] = "GH";
        authProperties.Items["currency"] = "GHS";
        authProperties.Items["businessType"] = "Food";

        var scheme = new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, authProperties, "Google");

        var ticketContext = new TicketReceivedContext(httpContext, scheme, new RemoteAuthenticationOptions(), ticket)
        {
            Properties = authProperties,
            Principal = principal,
        };

        await GoogleOAuthHandler.OnTicketReceived(ticketContext);

        var userManager = sp.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync("newoauthuser@example.com");

        user.Should().NotBeNull();
        user!.TenantId.Should().NotBeNull();
        user.IsOwner.Should().BeTrue();
        user.Name.Should().Be("Jane OAuth");

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("Jane's Bakery");
        tenant.Country.Should().Be("GH");
        tenant.Currency.Should().Be("GHS");
        tenant.BusinessType.Should().Be(BusinessType.Food);

        var subscription = await db.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        subscription.Should().NotBeNull();
        subscription!.Status.Should().Be(SubscriptionStatus.Trialing);

        ticketContext.ReturnUri.Should().Contain("accessToken=");
        ticketContext.ReturnUri.Should().Contain("refreshToken=");
    }

    [Fact]
    public async Task OnTicketReceived_Provisions_Tenant_For_Existing_User_Without_Tenant()
    {
        var (sp, db, connection) = CreateServices();
        using var _ = connection;
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        sp.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var userManager = sp.GetRequiredService<UserManager<User>>();
        var existing = new User
        {
            UserName = "orphaned-oauth@example.com",
            Email = "orphaned-oauth@example.com",
            EmailConfirmed = true,
            Name = "Orphan OAuth",
            LocationScope = "*",
            Status = UserStatus.Active,
        };
        (await userManager.CreateAsync(existing)).Succeeded.Should().BeTrue();

        var identity = new ClaimsIdentity("Google");
        identity.AddClaim(new Claim(ClaimTypes.Email, existing.Email!));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "google-sub-orphaned"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Orphan OAuth"));
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties();
        authProperties.Items["returnUrl"] = "http://localhost:5173/auth/google-callback";

        var scheme = new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, authProperties, "Google");
        var ticketContext = new TicketReceivedContext(httpContext, scheme, new RemoteAuthenticationOptions(), ticket)
        {
            Properties = authProperties,
            Principal = principal,
        };

        await GoogleOAuthHandler.OnTicketReceived(ticketContext);

        var stored = await userManager.FindByEmailAsync(existing.Email!);
        stored.Should().NotBeNull();
        stored!.TenantId.Should().NotBeNull();
        stored.IsOwner.Should().BeTrue();
        stored.RoleId.Should().NotBeNull();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == stored.TenantId);
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("Orphan OAuth's Organization");

        ticketContext.ReturnUri.Should().Contain("accessToken=");
        ticketContext.ReturnUri.Should().Contain("refreshToken=");
    }
}
