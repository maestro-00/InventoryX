using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Tenancy;
using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Tenancy;

/// <summary>T043 - tenant profile and removable sample-data lifecycle.</summary>
public sealed class TenantProfileCommandTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public TenantProfileCommandTests()
    {
        _db = new TestDb(_tenantId, "owner-1");
    }

    [Fact]
    public async Task Valuation_change_requires_confirmation()
    {
        await using var context = _db.CreateContext();
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop" });
        await context.SaveChangesAsync();
        var handler = new UpdateTenantCommandHandler(context, _db.TenantContext);

        var update = () => handler.Handle(new UpdateTenantCommand
        {
            ValuationMethod = "Fifo",
        }, CancellationToken.None);

        await update.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Sample_data_can_be_loaded_and_removed_in_one_action()
    {
        await using var context = _db.CreateContext();
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop" });
        await context.SaveChangesAsync();

        await new LoadSampleDataCommandHandler(context, _db.TenantContext)
            .Handle(new LoadSampleDataCommand(), CancellationToken.None);

        (await context.Products.CountAsync(p => p.IsSampleData)).Should().Be(3);
        (await context.Tenants.SingleAsync()).SampleDataLoaded.Should().BeTrue();

        await new RemoveSampleDataCommandHandler(context, _db.TenantContext)
            .Handle(new RemoveSampleDataCommand(), CancellationToken.None);

        (await context.Products.CountAsync(p => p.IsSampleData)).Should().Be(0);
        (await context.Categories.CountAsync(c => c.Name.StartsWith("Sample - "))).Should().Be(0);
        (await context.Tenants.SingleAsync()).SampleDataLoaded.Should().BeFalse();
    }

    public void Dispose() => _db.Dispose();
}
