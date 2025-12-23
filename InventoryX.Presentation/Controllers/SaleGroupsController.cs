using InventoryX.Application;
using InventoryX.Application.Commands.Requests.SaleGroups;
using InventoryX.Application.DTOs.SaleGroups;
using InventoryX.Application.Queries.Requests.SaleGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SaleGroupsController(IMediator mediator) : Controller
{
    [HttpGet("{id}")]
    public async Task<ActionResult> Get(int id)
    {
        var response = await mediator.Send(new GetSaleGroupRequest(id));
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var response = await mediator.Send(new GetAllSaleGroupsRequest());
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Add(SaleGroupCommandDto saleGroup)
    {
        if (ModelState.IsValid)
        {
            var response = await mediator.Send(new CreateSaleGroupCommand { SaleGroupCommandDto = saleGroup });
            return StatusCode(response.StatusCode, response);
        }
        return BadRequest(ModelState);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var response = await mediator.Send(new DeleteSaleGroupCommand { Id = id });
        return StatusCode(response.StatusCode, response);
    }
}
