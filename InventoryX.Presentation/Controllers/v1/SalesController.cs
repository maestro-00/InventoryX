using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Selling;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryX.Presentation.Swagger;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/sales")]
[Authorize]
[Tags("Sales")]
public sealed class SalesController(ISender sender) : ApiControllerBase
{
    public sealed record CreateSaleLineRequest(
        Guid ProductId,
        Guid? VariantId,
        Guid? BatchId,
        decimal Qty,
        decimal? UnitPrice,
        decimal LineDiscount,
        string? DiscountAuthorizedBy,
        string? Note);

    public sealed record CreateSalePaymentRequest(string Tender, decimal Amount, string? Reference);

    /// <summary>Public create-sale body. Offline-only flags are not accepted here.</summary>
    public sealed record CreateSaleRequest(
        Guid ClientSaleId,
        Guid RegisterId,
        Guid ShiftId,
        string Status = "Completed",
        List<CreateSaleLineRequest>? Lines = null,
        List<CreateSalePaymentRequest>? Payments = null,
        DateTime? OccurredAt = null);

    public sealed record CompleteHeldSaleRequest(List<CreateSalePaymentDto> Payments);
    public sealed record DeliverReceiptRequest(string Channel, string Destination);

    [HttpPost]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleDto>> Create(
        CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSaleCommand
        {
            ClientSaleId = request.ClientSaleId == Guid.Empty ? Guid.NewGuid() : request.ClientSaleId,
            RegisterId = request.RegisterId,
            ShiftId = request.ShiftId,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Completed" : request.Status,
            Lines = (request.Lines ?? []).Select(line => new CreateSaleLineDto
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                BatchId = line.BatchId,
                Qty = line.Qty,
                UnitPrice = line.UnitPrice,
                LineDiscount = line.LineDiscount,
                DiscountAuthorizedBy = line.DiscountAuthorizedBy,
                Note = line.Note,
            }).ToList(),
            Payments = (request.Payments ?? []).Select(payment => new CreateSalePaymentDto
            {
                Tender = payment.Tender,
                Amount = payment.Amount,
                Reference = payment.Reference,
            }).ToList(),
            OccurredAt = request.OccurredAt,
            OfflineOrigin = false,
        }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SaleDto>>> List(
        [FromQuery] GetSalesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpGet("held")]
    public async Task<ActionResult<List<SaleDto>>> Held(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetHeldSalesQuery(), cancellationToken));

    [HttpGet("{id:guid}/receipt")]
    public async Task<ActionResult<ReceiptDto>> Receipt(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSaleReceiptQuery(id), cancellationToken));

    [HttpPost("{id:guid}/receipt/deliver")]
    [LiveOnly("Email and SMS receipt delivery requires connectivity.")]
    public async Task<ActionResult<ReceiptDeliveryResultDto>> DeliverReceipt(
        Guid id,
        [FromBody] DeliverReceiptRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DeliverReceiptCommand
        {
            SaleId = id,
            Channel = request.Channel,
            Destination = request.Destination,
        }, cancellationToken));

    [HttpGet("held/{id:guid}")]
    public async Task<ActionResult<SaleDto>> RecallHeld(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetHeldSaleQuery { Id = id }, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<SaleDto>> CompleteHeld(
        Guid id,
        CompleteHeldSaleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CompleteHeldSaleCommand { SaleId = id, Payments = request.Payments }, cancellationToken));

    [HttpGet("lookup")]
    public async Task<ActionResult<List<SaleDto>>> Lookup(
        [FromQuery] string? receiptNumber,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new LookupSaleForReturnQuery { ReceiptNumber = receiptNumber, Search = search }, cancellationToken));

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<SaleDto>> Void(
        Guid id,
        [FromBody] VoidSaleRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new VoidSaleCommand { SaleId = id, Reason = request?.Reason }, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSaleQuery { Id = id }, cancellationToken));
}

public sealed record VoidSaleRequest(string? Reason);
