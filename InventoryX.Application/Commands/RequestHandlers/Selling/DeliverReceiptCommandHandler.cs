using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Selling
{
    public sealed class DeliverReceiptCommandHandler(
        IAppDbContext context,
        IReceiptDeliveryService deliveryService) : IRequestHandler<DeliverReceiptCommand, ReceiptDeliveryResultDto>
    {
        public async Task<ReceiptDeliveryResultDto> Handle(DeliverReceiptCommand request, CancellationToken cancellationToken)
        {
            var receipt = await context.Receipts
                .Include(r => r.Sale)
                .FirstOrDefaultAsync(r => r.SaleId == request.SaleId, cancellationToken)
                ?? throw new Exceptions.NotFoundException("No receipt found for this sale.");

            if (!Enum.TryParse<ReceiptChannel>(request.Channel, true, out var channel))
                throw new FluentValidation.ValidationException(
                    $"Unknown channel '{request.Channel}'. Valid: Email, Sms, Qr.");

            var log = new ReceiptDeliveryLog
            {
                TenantId = receipt.TenantId,
                ReceiptId = receipt.Id,
                Channel = channel,
                Destination = request.Destination,
                DeliveredAt = DateTime.UtcNow,
            };
            context.ReceiptDeliveryLogs.Add(log);

            try
            {
                await deliveryService.DeliverAsync(receipt, channel, request.Destination, cancellationToken);
                log.Success = true;
                await context.SaveChangesAsync(cancellationToken);
                return new ReceiptDeliveryResultDto
                {
                    SaleId = request.SaleId,
                    Channel = channel.ToString(),
                    Destination = request.Destination,
                    Success = true,
                };
            }
            catch (Exception exception)
            {
                log.ErrorMessage = exception.Message;
                await context.SaveChangesAsync(cancellationToken);
                throw;
            }
        }
    }
}
