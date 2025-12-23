using MediatR;

namespace InventoryX.Application.Queries.Requests.SaleGroups;

public record GetAllSaleGroupsRequest() : IRequest<ApiResponse>;
