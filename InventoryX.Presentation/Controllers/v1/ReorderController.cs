using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Queries.Requests.Purchasing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/reorder")]
[Authorize(Roles = "Owner,Administrator,Manager")]
public sealed class ReorderController(ISender sender) : ApiControllerBase
{
    /// <summary>Items at/below reorder point grouped by supplier, with suggested qty.</summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ReorderSuggestionsDto), StatusCodes.Status200OK)]
    public Task<ReorderSuggestionsDto> GetSuggestions(
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken) =>
        sender.Send(new GetReorderSuggestionsQuery(locationId), cancellationToken);

    /// <summary>Create draft POs from selected reorder suggestions.</summary>
    [HttpPost("suggestions/apply")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public Task<IReadOnlyList<PurchaseOrderDto>> ApplySuggestions(
        [FromBody] ApplyReorderSuggestionsCommand command,
        CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);
}
