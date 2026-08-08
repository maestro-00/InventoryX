using InventoryX.Application.Commands.Requests.Purchasing; using InventoryX.Application.Queries.Requests.Purchasing; using InventoryX.Application.Repository; using MediatR; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace InventoryX.Presentation.Controllers.v1;
[Route("api/v1/suppliers")][Authorize]
public sealed class SuppliersController(ISender sender,IAppDbContext context):ApiControllerBase{
 public sealed record SupplierProductInput(Guid ProductId,string? SupplierCode,decimal Price);
 [HttpGet] public async Task<ActionResult> List(CancellationToken ct)=>Ok(await context.Suppliers.AsNoTracking().Select(x=>new SupplierDto(x.Id,x.Name,x.Email,x.Phone)).ToListAsync(ct));
 [HttpPost] public async Task<ActionResult<SupplierDto>> Create(CreateSupplierCommand command,CancellationToken ct)=>Ok(await sender.Send(command,ct));
 [HttpGet("{id:guid}/performance")]
 [ProducesResponseType(typeof(SupplierPerformanceDto), StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 public Task<SupplierPerformanceDto> GetPerformance(Guid id, CancellationToken ct) =>
     sender.Send(new GetSupplierPerformanceQuery(id), ct);
 [HttpGet("{id:guid}/products")] public async Task<ActionResult> Products(Guid id,CancellationToken ct)=>Ok(await context.SupplierProducts.AsNoTracking().Where(x=>x.SupplierId==id).OrderBy(x=>x.ProductId).ToListAsync(ct));
 [HttpPut("{id:guid}/products")] public async Task<ActionResult> PutProducts(Guid id,List<SupplierProductInput> items,CancellationToken ct){
  var existing=await context.SupplierProducts.Where(x=>x.SupplierId==id).ToListAsync(ct); context.SupplierProducts.RemoveRange(existing);
  context.SupplierProducts.AddRange(items.Select(x=>new InventoryX.Domain.Models.Purchasing.SupplierProduct{SupplierId=id,ProductId=x.ProductId,SupplierCode=x.SupplierCode,LastPrice=x.Price,PriceUpdatedAt=DateTime.UtcNow}));
  await context.SaveChangesAsync(ct); return NoContent(); }
 [HttpGet("{id:guid}/orders")] public async Task<ActionResult> Orders(Guid id,CancellationToken ct)=>Ok(await context.PurchaseOrders.AsNoTracking().Where(x=>x.SupplierId==id).OrderByDescending(x=>x.CreatedAt).ToListAsync(ct));
}
