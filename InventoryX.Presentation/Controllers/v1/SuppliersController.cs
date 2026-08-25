using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.Queries.Requests.Purchasing;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/suppliers")]
[Authorize]
[Tags("Suppliers")]
public sealed class SuppliersController(ISender sender, IAppDbContext context) : ApiControllerBase
{
    public sealed record CreateSupplierRequest(
        string Name,
        string? Email,
        string? Phone,
        string? Address,
        string? Currency,
        int LeadTimeDays,
        string? PaymentTerms);

    public sealed record UpdateSupplierRequest(
        string? Name,
        string? Email,
        string? Phone,
        string? Address,
        string? Currency,
        int? LeadTimeDays,
        string? PaymentTerms);

    public sealed record SupplierProductInput(Guid ProductId, string? SupplierCode, decimal Price);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupplierDto>>> List(
        [FromQuery] GetSuppliersQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSupplierCommand(
            request.Name, request.Email, request.Phone, request.Address,
            request.Currency, request.LeadTimeDays, request.PaymentTerms), cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> Update(
        Guid id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSupplierCommand
        {
            Id = id,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Currency = request.Currency,
            LeadTimeDays = request.LeadTimeDays,
            PaymentTerms = request.PaymentTerms,
            ExpectedRowVersion = ParseIfMatchRowVersion(),
        }, cancellationToken);
        SetETag(result.RowVersion);
        return Ok(result);
    }

    [HttpGet("{id:guid}/performance")]
    [ProducesResponseType(typeof(SupplierPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SupplierPerformanceDto> GetPerformance(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetSupplierPerformanceQuery(id), cancellationToken);

    [HttpGet("{id:guid}/products")]
    public async Task<ActionResult> Products(Guid id, CancellationToken cancellationToken) =>
        Ok(await context.SupplierProducts.AsNoTracking().Where(x => x.SupplierId == id).OrderBy(x => x.ProductId).ToListAsync(cancellationToken));

    [HttpPut("{id:guid}/products")]
    public async Task<ActionResult> PutProducts(Guid id, List<SupplierProductInput> items, CancellationToken cancellationToken)
    {
        var existing = await context.SupplierProducts.Where(x => x.SupplierId == id).ToListAsync(cancellationToken);
        context.SupplierProducts.RemoveRange(existing);
        context.SupplierProducts.AddRange(items.Select(x => new SupplierProduct
        {
            SupplierId = id,
            ProductId = x.ProductId,
            SupplierCode = x.SupplierCode,
            LastPrice = x.Price,
            PriceUpdatedAt = DateTime.UtcNow,
        }));
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/orders")]
    public async Task<ActionResult> Orders(Guid id, CancellationToken cancellationToken) =>
        Ok(await context.PurchaseOrders.AsNoTracking().Where(x => x.SupplierId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken));
}
