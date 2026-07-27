using InventoryX.Application.Commands.Requests.Catalog;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/products")]
[Authorize]
public sealed class ProductsController(ISender sender) : ApiControllerBase
{
    private bool CanViewProfit => User.IsInRole("Owner") || User.IsInRole("Administrator") || User.IsInRole("Manager");

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List([FromQuery] GetProductsQuery query, CancellationToken cancellationToken) =>
        Ok(await sender.Send(query with { IncludeCost = CanViewProfit }, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProductQuery { Id = id, IncludeCost = CanViewProfit }, cancellationToken));

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> ByBarcode(string barcode, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProductByBarcodeQuery { Barcode = barcode, IncludeCost = CanViewProfit }, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateProductCommand
        {
            Id = id,
            Name = command.Name,
            Description = command.Description,
            Sku = command.Sku,
            Barcode = command.Barcode,
            CategoryId = command.CategoryId,
            SellingPrice = command.SellingPrice,
            CostPrice = command.CostPrice,
            TaxTreatmentCode = command.TaxTreatmentCode,
            Status = command.Status,
            ReorderPoint = command.ReorderPoint,
            ReorderQuantity = command.ReorderQuantity,
            LeadTimeDays = command.LeadTimeDays,
        }, cancellationToken));

    [HttpPost("{id:guid}/variants")]
    public async Task<ActionResult<ProductDto>> AddVariants(Guid id, AddProductVariantsCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddProductVariantsCommand { ProductId = id, Variants = command.Variants }, cancellationToken));

    [HttpGet("tax-treatments")]
    public async Task<ActionResult<List<TaxTreatmentDto>>> TaxTreatments(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTaxTreatmentsQuery(), cancellationToken));
}
