using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Purchasing;
using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Common.Tests;
using InventoryX.Domain.Models.Purchasing;
using InventoryX.Domain.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Purchasing;

public sealed class PurchaseOrderStateTests : IDisposable
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public PurchaseOrderStateTests() => _db = new TestDb(_tenantId, "buyer-1");

    [Fact]
    public void State_machine_rejects_illegal_transitions_and_allows_cancellation_before_closure()
    {
        var order = NewOrder();

        var approveDraft = () => order.Approve("manager-1", DateTime.UtcNow);
        approveDraft.Should().Throw<InvalidOperationException>();

        order.Submit(requiresApproval: true, DateTime.UtcNow);
        order.Status.Should().Be(PurchaseOrderStatus.AwaitingApproval);
        order.Approve("manager-1", DateTime.UtcNow);
        order.Status.Should().Be(PurchaseOrderStatus.Sent);
        order.Cancel("Supplier unavailable", DateTime.UtcNow);
        order.Status.Should().Be(PurchaseOrderStatus.Cancelled);

        var cancelAgain = () => order.Cancel("Again", DateTime.UtcNow);
        cancelAgain.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Submit_at_threshold_persists_awaiting_approval_and_returns_approval_hint()
    {
        await using var context = _db.CreateContext();
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop", PoApprovalThreshold = 100m });
        var supplier = new Supplier { Name = "Acme" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var create = new PurchaseOrderCommandHandler(context);
        var created = await create.Handle(new CreatePurchaseOrderCommand
        {
            SupplierId = supplier.Id,
            Origin = PurchaseOrderOrigin.Manual,
            Lines = [new PurchaseOrderLineInput(Guid.NewGuid(), null, "Sugar", 10m, 10m)],
        }, CancellationToken.None);

        var submit = new SubmitPurchaseOrderCommandHandler(context, _db.TenantContext);
        var act = () => submit.Handle(new SubmitPurchaseOrderCommand(created.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ApprovalRequiredException>();
        error.Which.PendingEntityId.Should().Be(created.Id);
        (await context.PurchaseOrders.SingleAsync()).Status.Should().Be(PurchaseOrderStatus.AwaitingApproval);
    }

    [Fact]
    public async Task Submit_below_threshold_sends_directly_and_records_origin()
    {
        await using var context = _db.CreateContext();
        context.Tenants.Add(new Tenant { Id = _tenantId, Name = "Shop", PoApprovalThreshold = 101m });
        var supplier = new Supplier { Name = "Acme" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var handler = new PurchaseOrderCommandHandler(context);
        var created = await handler.Handle(new CreatePurchaseOrderCommand
        {
            SupplierId = supplier.Id,
            Origin = PurchaseOrderOrigin.ReorderSuggestion,
            OriginReferenceId = Guid.NewGuid(),
            Lines = [new PurchaseOrderLineInput(Guid.NewGuid(), null, "Sugar", 10m, 10m)],
        }, CancellationToken.None);

        var submitted = await new SubmitPurchaseOrderCommandHandler(context, _db.TenantContext)
            .Handle(new SubmitPurchaseOrderCommand(created.Id), CancellationToken.None);

        submitted.Status.Should().Be(PurchaseOrderStatus.Sent);
        submitted.Origin.Should().Be(PurchaseOrderOrigin.ReorderSuggestion);
    }

    private static PurchaseOrder NewOrder() => new()
    {
        SupplierId = Guid.NewGuid(),
        Lines = [new PurchaseOrderLine { ProductId = Guid.NewGuid(), Description = "Sugar", OrderedQty = 1m, UnitCost = 2m }],
    };

    public void Dispose() => _db.Dispose();
}
