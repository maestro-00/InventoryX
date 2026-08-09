using FluentAssertions;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Tests.Data;

/// <summary>
/// T008 — constitution G6 release-blocking rule: tenant-owned rows must never
/// leak across tenants (global query filter) and cross-tenant writes must be
/// rejected by the SaveChanges interceptor.
/// </summary>
public sealed class TenantIsolationTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly SqliteConnection _connection;

    private sealed class FixedTenantContext(Guid? tenantId, string? userId = "test-user") : ITenantContext
    {
        public Guid? TenantId { get; set; } = tenantId;
        public string? UserId { get; set; } = userId;
        public string? Role { get; set; }
        public string? LocationScope { get; set; }
    }

    public TenantIsolationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var context = CreateContext(new FixedTenantContext(null));
        context.Database.EnsureCreated();
    }

    private AppDbContext CreateContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new TenantSaveChangesInterceptor(tenantContext))
            .Options;
        return new AppDbContext(options, tenantContext);
    }

    private static AuditLogEntry NewEntry(string action) => new()
    {
        Actor = "tester",
        Action = action,
        EntityType = "Test",
        EntityId = Guid.NewGuid().ToString(),
    };

    private static Notification NewNotification(string title) => new()
    {
        ConsolidationKey = title,
        Title = title,
        LastRaisedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Query_filter_returns_only_current_tenants_rows()
    {
        await using (var contextA = CreateContext(new FixedTenantContext(TenantA)))
        {
            contextA.AuditLogEntries.Add(NewEntry("a-action"));
            await contextA.SaveChangesAsync();
        }

        await using (var contextB = CreateContext(new FixedTenantContext(TenantB)))
        {
            contextB.AuditLogEntries.Add(NewEntry("b-action"));
            await contextB.SaveChangesAsync();
        }

        await using var reader = CreateContext(new FixedTenantContext(TenantA));
        var visible = await reader.AuditLogEntries.ToListAsync();

        visible.Should().OnlyContain(e => e.TenantId == TenantA);
        visible.Should().ContainSingle(e => e.Action == "a-action");
    }

    [Fact]
    public async Task Added_entities_are_stamped_with_current_tenant_and_audit_fields()
    {
        await using var context = CreateContext(new FixedTenantContext(TenantA));
        var entry = NewEntry("stamped");
        context.AuditLogEntries.Add(entry);
        await context.SaveChangesAsync();

        entry.TenantId.Should().Be(TenantA);
        entry.CreatedAt.Should().NotBe(default);
        entry.CreatedBy.Should().Be("test-user");
    }

    [Fact]
    public async Task Adding_an_entity_stamped_for_another_tenant_is_rejected()
    {
        await using var context = CreateContext(new FixedTenantContext(TenantA));
        var foreign = NewEntry("cross-tenant");
        foreign.TenantId = TenantB;
        context.AuditLogEntries.Add(foreign);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cross-tenant*");
    }

    [Fact]
    public async Task Modifying_another_tenants_entity_is_rejected()
    {
        Guid foreignId;
        await using (var contextB = CreateContext(new FixedTenantContext(TenantB)))
        {
            var notification = NewNotification("owned-by-b");
            contextB.Notifications.Add(notification);
            await contextB.SaveChangesAsync();
            foreignId = notification.Id;
        }

        await using var contextA = CreateContext(new FixedTenantContext(TenantA));
        var smuggled = NewNotification("hijack");
        smuggled.Id = foreignId;
        smuggled.TenantId = TenantB;
        contextA.Notifications.Attach(smuggled);
        contextA.Entry(smuggled).State = EntityState.Modified;

        var act = () => contextA.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cross-tenant*");
    }

    [Fact]
    public async Task Query_with_no_tenant_context_sees_nothing_for_tenant_owned_sets()
    {
        await using (var contextA = CreateContext(new FixedTenantContext(TenantA)))
        {
            contextA.AuditLogEntries.Add(NewEntry("a-only"));
            await contextA.SaveChangesAsync();
        }

        await using var anonymous = CreateContext(new FixedTenantContext(null));
        var visible = await anonymous.AuditLogEntries.ToListAsync();

        visible.Should().BeEmpty();
    }

    public void Dispose() => _connection.Dispose();
}
