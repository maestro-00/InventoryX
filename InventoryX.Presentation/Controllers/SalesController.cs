using InventoryX.Application.Commands.Requests.Sales;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Application.Queries.Requests.Sales;
using MediatR;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController(IMediator mediator) : Controller
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(int id)
        {
            var response = await _mediator.Send(new GetSaleRequest { Id = id });
            return StatusCode(response.StatusCode,response);
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var response = await _mediator.Send(new GetAllSaleRequest());
            return StatusCode(response.StatusCode,response);
        }

        [HttpPost]
        public async Task<ActionResult> Add(SaleCommandDto Sale)
        {
            if (ModelState.IsValid)
            {
                var response = await _mediator.Send(new CreateSaleCommand { NewSaleDto = Sale });
                return StatusCode(response.StatusCode,response);
            }
            return BadRequest(ModelState);
        }
        [HttpPut]
        [Route("{id}")]
        public async Task<ActionResult> Update(int id, SaleCommandDto Sale)
        {
            if (ModelState.IsValid)
            {
                var response = await _mediator.Send(new UpdateSaleCommand { Id = id, SaleDto = Sale });
                return StatusCode(response.StatusCode,response);
            }
            return BadRequest(ModelState);
        }
        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var response = await _mediator.Send(new DeleteSaleCommand { Id = id });
            return StatusCode(response.StatusCode,response);
        }

    }
}
