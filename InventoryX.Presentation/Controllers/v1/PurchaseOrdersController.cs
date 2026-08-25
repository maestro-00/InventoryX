using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/purchase-orders")]
[Authorize]
[Tags("PurchaseOrders")]
public sealed class PurchaseOrdersController(ISender sender) : ApiControllerBase
{
    public sealed record CreatePurchaseOrderRequest(
        Guid SupplierId,
        Guid DeliverToLocationId,
        PurchaseOrderOrigin Origin,
        Guid? OriginReferenceId,
        DateTime? RequiredBy,
        string? Notes,
        List<PurchaseOrderLineInput> Lines);

    public sealed record UpdatePurchaseOrderRequest(
        Guid DeliverToLocationId,
        DateTime? RequiredBy,
        string? Notes,
        List<PurchaseOrderLineInput> Lines);

    [HttpGet]
    public Task<Application.DTOs.Common.PagedResult<PurchaseOrderDto>> List(PurchaseOrderStatus? status, Guid? supplierId, bool overdue = false, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) =>
        sender.Send(new GetPurchaseOrdersQuery(status, supplierId, overdue, page, pageSize), cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePurchaseOrderCommand
        {
            SupplierId = request.SupplierId,
            DeliverToLocationId = request.DeliverToLocationId,
            Origin = request.Origin,
            OriginReferenceId = request.OriginReferenceId,
            RequiredBy = request.RequiredBy,
            Notes = request.Notes,
            Lines = request.Lines ?? [],
        }, cancellationToken);
        SetETag(result.RowVersion);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderDto>> Update(Guid id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePurchaseOrderCommand
        {
            Id = id,
            DeliverToLocationId = request.DeliverToLocationId,
            RequiredBy = request.RequiredBy,
            Notes = request.Notes,
            Lines = request.Lines ?? [],
            ExpectedRowVersion = ParseIfMatchRowVersion(),
        }, cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public Task<PurchaseOrderDto> Submit(Guid id, CancellationToken cancellationToken) => sender.Send(new SubmitPurchaseOrderCommand(id), cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public Task<PurchaseOrderDto> Approve(Guid id, CancellationToken cancellationToken) => sender.Send(new ApprovePurchaseOrderCommand(id), cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public Task<PurchaseOrderDto> Reject(Guid id, CancellationToken cancellationToken) => sender.Send(new RejectPurchaseOrderCommand(id), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<PurchaseOrderDto> Cancel(Guid id, CancelPurchaseOrderRequest request, CancellationToken cancellationToken) => sender.Send(new CancelPurchaseOrderCommand(id, request.Reason), cancellationToken);

    [HttpPost("{id:guid}/send")]
    public Task<Application.Services.IServices.PurchaseOrderEmailResult> Send(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new SendPurchaseOrderCommand(id), cancellationToken);

    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var document = await sender.Send(new GetPurchaseOrderPdfQuery(id), cancellationToken);
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPost("{id:guid}/receipts")]
    public Task<GoodsReceiptDto> RecordReceipt(Guid id, RecordGoodsReceiptCommand command, CancellationToken cancellationToken) =>
        sender.Send(new RecordGoodsReceiptCommand { PurchaseOrderId = id, LocationId = command.LocationId, Notes = command.Notes, Lines = command.Lines }, cancellationToken);

    [HttpPost("{id:guid}/close-short")]
    public Task<PurchaseOrderDto> CloseShort(Guid id, ClosePurchaseOrderShortRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ClosePurchaseOrderShortCommand(id, request.Reason), cancellationToken);
}

public sealed record CancelPurchaseOrderRequest(string Reason);
public sealed record ClosePurchaseOrderShortRequest(string Reason);
