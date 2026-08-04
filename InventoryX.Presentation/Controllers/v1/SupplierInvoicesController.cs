using InventoryX.Application.Commands.Requests.Purchasing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1")]
[Authorize(Roles = "Owner,Administrator,Manager")]
public sealed class SupplierInvoicesController(ISender sender) : ApiControllerBase
{
    /// <summary>Record a supplier invoice against a PO; response flags line price variances vs ordered.</summary>
    [HttpPost("supplier-invoices")]
    [ProducesResponseType(typeof(SupplierInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupplierInvoiceDto> RecordInvoice(
        [FromBody] RecordSupplierInvoiceCommand command,
        CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);

    /// <summary>Allocate freight/duty/clearing/insurance across receipt lines by value; recalculates item true cost.</summary>
    [HttpPost("goods-receipts/{id:guid}/landed-costs")]
    [ProducesResponseType(typeof(LandedCostAllocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<LandedCostAllocationDto> AllocateLandedCosts(
        Guid id,
        [FromBody] AllocateLandedCostsCommand command,
        CancellationToken cancellationToken)
    {
        command.GoodsReceiptId = id;
        return sender.Send(command, cancellationToken);
    }
}
