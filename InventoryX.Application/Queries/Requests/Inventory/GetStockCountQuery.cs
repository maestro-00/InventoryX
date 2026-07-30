using InventoryX.Application.Commands.Requests.Inventory;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Inventory;

public record GetStockCountQuery(Guid CountId) : IRequest<StockCountResult>;
