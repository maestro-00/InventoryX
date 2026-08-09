using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InventoryX.Common.Tests;

/// <summary>Mutable tenant context stub for tests.</summary>
public class TestTenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public string? LocationScope { get; set; }
}

/// <summary>
/// Shared Sqlite-in-memory AppDbContext fixture: one open connection per
/// instance, schema created once, contexts created per tenant context.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestTenantContext TenantContext { get; }

    public TestDb(Guid? tenantId = null, string? userId = "test-user")
    {
        TenantContext = new TestTenantContext { TenantId = tenantId, UserId = userId };
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a context bound to this fixture's tenant context (or an override).</summary>
    public AppDbContext CreateContext(ITenantContext? tenantContext = null)
    {
        var effective = tenantContext ?? TenantContext;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new TenantSaveChangesInterceptor(effective))
            .Options;
        return new AppDbContext(options, effective);
    }

    /// <summary>Real UserManager over the given context for identity-dependent handlers.</summary>
    public static UserManager<User> CreateUserManager(AppDbContext context)
    {
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<User>(context);
        var identityOptions = Options.Create(new IdentityOptions
        {
            Password = { RequireDigit = false, RequireLowercase = false, RequireUppercase = false, RequireNonAlphanumeric = false, RequiredLength = 6 },
            User = { RequireUniqueEmail = true },
        });
        return new UserManager<User>(
            store, identityOptions, new PasswordHasher<User>(),
            [new UserValidator<User>()], [new PasswordValidator<User>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<User>>.Instance);
    }

    public void Dispose() => _connection.Dispose();
}
