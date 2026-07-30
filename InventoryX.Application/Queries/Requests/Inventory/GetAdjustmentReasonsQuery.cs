using MediatR;

namespace InventoryX.Application.Queries.Requests.Inventory;

public record AdjustmentReasonDto(Guid Id, string Code, string Name, bool IsSystem);
public record GetAdjustmentReasonsQuery : IRequest<List<AdjustmentReasonDto>>;
