using InventoryX.Application.Commands.Requests.Purchasing; using InventoryX.Application.Queries.Requests.Purchasing; using InventoryX.Application.Repository; using MediatR; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace InventoryX.Presentation.Controllers.v1;
[Route("api/v1/suppliers")][Authorize]
public sealed class SuppliersController(ISender sender,IAppDbContext context):ApiControllerBase{
 [HttpGet] public async Task<ActionResult> List(CancellationToken ct)=>Ok(await context.Suppliers.AsNoTracking().Select(x=>new SupplierDto(x.Id,x.Name,x.Email,x.Phone)).ToListAsync(ct));
 [HttpPost] public async Task<ActionResult<SupplierDto>> Create(CreateSupplierCommand command,CancellationToken ct)=>Ok(await sender.Send(command,ct));
 [HttpGet("{id:guid}/performance")]
 [ProducesResponseType(typeof(SupplierPerformanceDto), StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 public Task<SupplierPerformanceDto> GetPerformance(Guid id, CancellationToken ct) =>
     sender.Send(new GetSupplierPerformanceQuery(id), ct);
}
