using InventoryX.Application.Queries.Requests.Sync;
using InventoryX.Application.Commands.Requests.Sync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/sync")]
[Authorize(Policy = "RegisterTokenOrUser")]
public sealed class SyncController(ISender sender) : ApiControllerBase
{
    [HttpPost("sales")]
    public async Task<ActionResult<List<OfflineSaleIngestResult>>> IngestSales(
        IngestOfflineSalesCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Sales.Count > 0)
            HttpContext.Items["RegisterToken.RegisterId"] = command.Sales[0].RegisterId;
        return Ok(await sender.Send(command, cancellationToken));
    }

    [HttpGet("conflicts")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<List<InventoryX.Application.DTOs.Selling.SaleDto>>> Conflicts(
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSyncConflictsQuery(), cancellationToken));

    [HttpPost("conflicts/{saleId:guid}/resolve")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<SyncConflictResult>> ResolveConflict(
        Guid saleId,
        ResolveSyncConflictCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ResolveSyncConflictCommand
        {
            SaleId = saleId,
            Resolution = command.Resolution,
            ReasonCode = command.ReasonCode,
            Note = command.Note,
            Adjustments = command.Adjustments,
        }, cancellationToken));

    [HttpGet("rejected")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<List<RejectedOfflineSaleDto>>> Rejected(
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListRejectedOfflineSalesQuery(), cancellationToken));

    [HttpPost("rejected/{rejectedSaleId:guid}/resolve")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<ResolveRejectedOfflineSaleResult>> ResolveRejected(
        Guid rejectedSaleId,
        ResolveRejectedOfflineSaleCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ResolveRejectedOfflineSaleCommand
        {
            RejectedSaleId = rejectedSaleId,
            Resolution = command.Resolution,
            LinkedReconciliationSaleId = command.LinkedReconciliationSaleId,
            Note = command.Note,
        }, cancellationToken));

    [HttpGet("snapshot")]
    public async Task<ActionResult<SyncSnapshotDto>> Snapshot(
        [FromQuery] Guid registerId,
        [FromQuery] string? watermark,
        CancellationToken cancellationToken)
    {
        HttpContext.Items["RegisterToken.RegisterId"] = registerId;
        return Ok(await sender.Send(new GetSyncSnapshotQuery(registerId, watermark), cancellationToken));
    }
}
