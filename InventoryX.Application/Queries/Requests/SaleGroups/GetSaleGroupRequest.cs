using MediatR;

namespace InventoryX.Application.Queries.Requests.SaleGroups;

public record GetSaleGroupRequest(int Id) : IRequest<ApiResponse>;
