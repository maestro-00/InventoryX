using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling
{
    /// <summary>
    /// Deliver a receipt via the specified channel (Email|Sms|Qr).
    /// Logs the delivery attempt for auditability.
    /// </summary>
    public class DeliverReceiptCommand : IRequest<ReceiptDeliveryResultDto>, IAuditedCommand
    {
        public Guid SaleId { get; init; }
        public required string Channel { get; init; }
        public required string Destination { get; init; }
        public string AuditAction => "receipt.deliver";
        public string AuditEntityType => "Receipt";
        public string AuditEntityId => SaleId.ToString();
    }

    public class ReceiptDeliveryResultDto
    {
        public Guid SaleId { get; init; }
        public string Channel { get; init; } = string.Empty;
        public string Destination { get; init; } = string.Empty;
        public bool Success { get; init; }
        public string? Message { get; init; }
    }
}
