using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Application.Commands.Requests.Sync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/sync")]
[Authorize]
public sealed class SyncController(ISender sender) : ApiControllerBase
{
    [HttpPost("sales")]
    public async Task<ActionResult<List<OfflineSaleIngestResult>>> IngestSales(
        IngestOfflineSalesCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpGet("snapshot")]
    public async Task<ActionResult<SyncSnapshotDto>> Snapshot(
        [FromQuery] Guid registerId,
        [FromQuery] string? watermark,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSyncSnapshotQuery(registerId, watermark), cancellationToken));
}
