using InventoryX.Application.Commands.Requests.Catalog;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.Queries.Requests.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/categories")]
[Authorize]
public sealed class CategoriesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCategoriesQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryCommand command, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateCategoryCommand { Id = id, Name = command.Name, ParentId = command.ParentId }, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
