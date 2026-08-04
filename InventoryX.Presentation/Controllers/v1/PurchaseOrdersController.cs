using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/purchase-orders")]
[Authorize]
public sealed class PurchaseOrdersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public Task<Application.DTOs.Common.PagedResult<PurchaseOrderDto>> List(PurchaseOrderStatus? status, Guid? supplierId, bool overdue = false, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) =>
        sender.Send(new GetPurchaseOrdersQuery(status, supplierId, overdue, page, pageSize), cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    public Task<PurchaseOrderDto> Update(Guid id, UpdatePurchaseOrderCommand command, CancellationToken cancellationToken) =>
        sender.Send(new UpdatePurchaseOrderCommand { Id = id, DeliverToLocationId = command.DeliverToLocationId, RequiredBy = command.RequiredBy, Notes = command.Notes, Lines = command.Lines }, cancellationToken);

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
