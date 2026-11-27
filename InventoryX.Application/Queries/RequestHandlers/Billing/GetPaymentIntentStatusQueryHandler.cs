using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Billing;

public sealed class GetPaymentIntentStatusQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPaymentIntentStatusQuery, PaymentIntentStatusDto>
{
    public async Task<PaymentIntentStatusDto> Handle(GetPaymentIntentStatusQuery request, CancellationToken cancellationToken)
    {
        var intent = await context.PaymentIntents.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Payment intent not found.");
        var latest = await context.PaymentAttempts.AsNoTracking()
            .Where(item => item.PaymentIntentId == intent.Id)
            .OrderByDescending(item => item.InitiatedAt)
            .Select(item => (PaymentAttemptStatus?)item.Status)
            .FirstOrDefaultAsync(cancellationToken);
        var status = intent.Status switch
        {
            PaymentIntentStatus.Succeeded => "succeeded",
            PaymentIntentStatus.Failed => "failed",
            PaymentIntentStatus.Cancelled => "cancelled",
            PaymentIntentStatus.Superseded => "superseded",
            _ when latest == PaymentAttemptStatus.AwaitingApproval => "awaiting_approval",
            _ => "pending",
        };
        return new PaymentIntentStatusDto(intent.Id, status, status is "pending" or "awaiting_approval" ? 5 : 0);
    }
}
