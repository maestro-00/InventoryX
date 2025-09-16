using InventoryX.Application.Commands.Requests.RetailStock; 
using InventoryX.Application.DTOs.RetailStock; 
using InventoryX.Application.Queries.Requests.RetailStock; 
using MediatR;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RetailStockController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(int id)
        {
            var response = await _mediator.Send(new GetRetailStockRequest { Id = id });
            return StatusCode(response.StatusCode,response);
        }
        [HttpGet]
        [Route("GetByInventoryItem/{id}")]
        public async Task<ActionResult> GetByInventoryItem(int id)
        {
            var response = await _mediator.Send(new GetByInventoryItemRetailStockRequest { Id = id });
            return StatusCode(response.StatusCode,response);
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var response = await _mediator.Send(new GetAllRetailStockRequest());
            return StatusCode(response.StatusCode,response);
        } 

        [HttpPut] 
        public async Task<ActionResult> Update(RetailStockCommandDto RetailStock)
        {
            if (ModelState.IsValid)
            {
                var response = await _mediator.Send(new UpdateRetailStockCommand { RetailStock = RetailStock });
                return StatusCode(response.StatusCode,response);
            }
            return BadRequest(ModelState);
        }
    }
}
