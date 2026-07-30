using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Sync;

public record GetSyncConflictsQuery : IRequest<List<SaleDto>>;
