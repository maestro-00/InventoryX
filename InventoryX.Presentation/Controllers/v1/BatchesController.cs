using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/batches")]
[Authorize(Roles = "Owner,Administrator,Manager,StockClerk")]
public sealed class BatchesController(ISender sender) : ApiControllerBase
{
    [HttpGet("{id:guid}/trace")]
    public Task<BatchTraceDto> Trace(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetBatchTraceQuery(id), cancellationToken);
}
