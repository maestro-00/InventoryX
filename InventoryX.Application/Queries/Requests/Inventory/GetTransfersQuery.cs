using InventoryX.Application.DTOs.Common;
using InventoryX.Domain.Models.Inventory;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Inventory;

public sealed record StockTransferDto(
    Guid Id,
    Guid FromLocationId,
    Guid ToLocationId,
    string Status,
    string? DiscrepancyReason,
    DateTime CreatedAt);

public sealed record GetTransfersQuery : PageRequest, IRequest<PagedResult<StockTransferDto>>
{
    public StockTransferStatus? Status { get; init; }
}
