using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.Queries.Requests.Catalog;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Auditing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1")]
[Authorize]
public sealed class ContractRoutesController(ISender sender, IAppDbContext context) : ApiControllerBase
{
    public sealed record CashMovementRequest(string Type, decimal Amount, string Reason);
    public sealed record CloseShiftRequest(decimal? ClosingCounted);

    [HttpGet("tax-treatments")]
    public Task<List<TaxTreatmentDto>> TaxTreatments(CancellationToken cancellationToken) =>
        sender.Send(new GetTaxTreatmentsQuery(), cancellationToken);

    [HttpGet("products/{id:guid}/batches")]
    public async Task<IActionResult> ProductBatches(Guid id, CancellationToken cancellationToken) => Ok(await (
        from batch in context.Batches.AsNoTracking()
        join stock in context.StockLevels.AsNoTracking() on batch.Id equals stock.BatchId
        where batch.ProductId == id && stock.QtyOnHand > 0
        orderby batch.ExpiresAt, batch.ReceivedAt
        select new { batch.Id, batch.BatchNumber, batch.ExpiresAt, batch.ReceivedAt, batch.UnitCost, stock.LocationId, remainingQty = stock.QtyOnHand })
        .ToListAsync(cancellationToken));

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts(CancellationToken cancellationToken) => Ok(await context.Notifications.AsNoTracking()
        .Where(item => item.Channel == NotificationChannel.InApp && item.ResolvedAt == null)
        .OrderByDescending(item => item.LastRaisedAt).ToListAsync(cancellationToken));

    [HttpPost("shifts/{shiftId:guid}/cash-movements")]
    public Task<InventoryX.Application.DTOs.Selling.CashMovementDto> CashMovement(Guid shiftId, CashMovementRequest request, CancellationToken cancellationToken) =>
        sender.Send(new RecordCashMovementCommand { ShiftId = shiftId, Type = request.Type, Amount = request.Amount, Reason = request.Reason }, cancellationToken);

    [HttpPost("shifts/{shiftId:guid}/close")]
    public Task<InventoryX.Application.DTOs.Selling.ShiftDto> CloseShift(Guid shiftId, CloseShiftRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CloseShiftCommand { ShiftId = shiftId, ClosingCounted = request.ClosingCounted }, cancellationToken);

    [HttpGet("shifts/{shiftId:guid}/z-report")]
    public Task<ZReportDto> ZReport(Guid shiftId, CancellationToken cancellationToken) =>
        sender.Send(new GetZReportQuery(shiftId), cancellationToken);
}
