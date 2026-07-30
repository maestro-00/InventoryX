using InventoryX.Application.Behaviors;
using InventoryX.Application.Commands.Requests.Selling;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Sync;

public record OfflineSaleIngestResult(Guid ClientSaleId, Guid? SaleId, string Status, string? Error = null);

public sealed class IngestOfflineSalesCommand : IRequest<List<OfflineSaleIngestResult>>, ITenantWriteCommand
{
    public List<CreateSaleCommand> Sales { get; init; } = [];
}
