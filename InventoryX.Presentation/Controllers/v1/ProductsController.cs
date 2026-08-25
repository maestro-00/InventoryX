using InventoryX.Application.Commands.Requests.Catalog;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryX.Presentation.Swagger;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/products")]
[Authorize]
[Tags("Products")]
public sealed class ProductsController(ISender sender) : ApiControllerBase
{
    private bool CanViewProfit => User.IsInRole("Owner") || User.IsInRole("Administrator") || User.IsInRole("Manager");

    public sealed record CreateProductRequest(
        string Name,
        string? Description = null,
        string? Sku = null,
        string? Barcode = null,
        Guid? CategoryId = null,
        string UnitOfMeasure = "Each",
        bool AllowFractional = false,
        decimal SellingPrice = 0,
        decimal CostPrice = 0,
        string? TaxTreatmentCode = null,
        string TrackingMode = "Simple",
        List<string>? VariantAttributes = null,
        decimal? ReorderPoint = null,
        decimal? ReorderQuantity = null,
        int? LeadTimeDays = null);

    public sealed record UpdateProductRequest(
        string? Name,
        string? Description,
        string? Sku,
        string? Barcode,
        Guid? CategoryId,
        decimal? SellingPrice,
        decimal? CostPrice,
        string? TaxTreatmentCode,
        string? Status,
        decimal? ReorderPoint,
        decimal? ReorderQuantity,
        int? LeadTimeDays);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List([FromQuery] GetProductsQuery query, CancellationToken cancellationToken) =>
        Ok(await sender.Send(query with { IncludeCost = CanViewProfit }, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductQuery { Id = id, IncludeCost = CanViewProfit }, cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> ByBarcode(string barcode, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProductByBarcodeQuery { Barcode = barcode, IncludeCost = CanViewProfit }, cancellationToken));

    [HttpGet("{id:guid}/availability")]
    [LiveOnly("Availability outside the cached register snapshot requires a live stock query.")]
    public async Task<ActionResult<InventoryX.Application.DTOs.Selling.ProductAvailabilityDto>> Availability(
        Guid id, [FromQuery] Guid? variantId, [FromQuery] Guid? locationId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new InventoryX.Application.Queries.Requests.Selling.GetProductAvailabilityQuery
        { ProductId = id, VariantId = variantId, LocationId = locationId }, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            CategoryId = request.CategoryId,
            UnitOfMeasure = string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? "Each" : request.UnitOfMeasure,
            AllowFractional = request.AllowFractional,
            SellingPrice = request.SellingPrice,
            CostPrice = request.CostPrice,
            TaxTreatmentCode = request.TaxTreatmentCode,
            TrackingMode = string.IsNullOrWhiteSpace(request.TrackingMode) ? "Simple" : request.TrackingMode,
            VariantAttributes = request.VariantAttributes,
            ReorderPoint = request.ReorderPoint,
            ReorderQuantity = request.ReorderQuantity,
            LeadTimeDays = request.LeadTimeDays,
        }, cancellationToken);
        SetETag(result.RowVersion);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProductCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            CategoryId = request.CategoryId,
            SellingPrice = request.SellingPrice,
            CostPrice = request.CostPrice,
            TaxTreatmentCode = request.TaxTreatmentCode,
            Status = request.Status,
            ReorderPoint = request.ReorderPoint,
            ReorderQuantity = request.ReorderQuantity,
            LeadTimeDays = request.LeadTimeDays,
            ExpectedRowVersion = ParseIfMatchRowVersion(),
        }, cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpPost("{id:guid}/variants")]
    public async Task<ActionResult<ProductDto>> AddVariants(Guid id, AddProductVariantsCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddProductVariantsCommand { ProductId = id, Variants = command.Variants }, cancellationToken));
}
