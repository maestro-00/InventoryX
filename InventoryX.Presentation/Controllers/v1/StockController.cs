using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Queries.Requests.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/stock")]
[Authorize]
public sealed class StockController(ISender sender) : ApiControllerBase
{
    private bool CanViewProfit =>
        User.IsInRole("Owner") || User.IsInRole("Administrator") || User.IsInRole("Manager");

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockLevelDto>>> List(
        [FromQuery] GetStockQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query with { IncludeCost = CanViewProfit }, cancellationToken));

    [HttpGet("movements")]
    [Authorize(Roles = "Owner,Administrator,Manager,StockClerk")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> Movements(
        [FromQuery] GetStockMovementsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpPost("movements/{id:guid}/correct")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<StockMovementDto>> CorrectMovement(
        Guid id,
        CorrectMovementCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CorrectMovementCommand
        {
            MovementId = id,
            CorrectedQtyDelta = command.CorrectedQtyDelta,
            ReasonCode = command.ReasonCode,
            Note = command.Note,
        }, cancellationToken));

    [HttpPost("adjustments")]
    public async Task<ActionResult<RecordStockAdjustmentResult>> RecordAdjustment(
        RecordStockAdjustmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));

    [HttpPost("adjustments/{id:guid}/approve")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<RecordStockAdjustmentResult>> ApproveAdjustment(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ApproveStockAdjustmentCommand { AdjustmentId = id }, cancellationToken));

    [HttpPost("adjustments/{id:guid}/reject")]
    [Authorize(Roles = "Owner,Administrator,Manager")]
    public async Task<ActionResult<RecordStockAdjustmentResult>> RejectAdjustment(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RejectStockAdjustmentCommand { AdjustmentId = id }, cancellationToken));

    [HttpGet("adjustment-reasons")]
    public async Task<ActionResult<List<AdjustmentReasonDto>>> AdjustmentReasons(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAdjustmentReasonsQuery(), cancellationToken));

    [HttpPost("consumption")]
    public async Task<ActionResult<RecordStockAdjustmentResult>> RecordConsumption(
        RecordConsumptionCommand command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));
}
