using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Billing;

public sealed class UpdateBillingContactCommandHandler(IAppDbContext context) : IRequestHandler<UpdateBillingContactCommand>
{
    public async Task Handle(UpdateBillingContactCommand request, CancellationToken cancellationToken)
    {
        var subscription = await SubscriptionCommands.CurrentAsync(context, cancellationToken);
        var tenant = await context.Tenants.SingleAsync(item => item.Id == subscription.TenantId, cancellationToken);
        tenant.BillingEmail = request.BillingEmail.Trim();
        tenant.BillingTaxNumber = request.TaxNumber?.Trim();
        await context.SaveChangesAsync(cancellationToken);
    }
}
