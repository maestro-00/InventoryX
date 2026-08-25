using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.DTOs.Common;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Purchasing;

public sealed record GetSuppliersQuery : PageRequest, IRequest<PagedResult<SupplierDto>>;
