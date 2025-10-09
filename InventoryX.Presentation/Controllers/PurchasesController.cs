using InventoryX.Application.Commands.Requests.Purchases;
using InventoryX.Application.DTOs.Purchases;
using InventoryX.Application.Queries.Requests.Purchases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchasesController(IMediator mediator) : Controller
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(int id)
        {
            var response = await _mediator.Send(new GetPurchaseRequest { Id = id });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var response = await _mediator.Send(new GetAllPurchaseRequest());
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        public async Task<ActionResult> Add(PurchaseCommandDto Purchase)
        {
            if (ModelState.IsValid)
            {
                var response = await _mediator.Send(new CreatePurchaseCommand { NewPurchaseDto = Purchase });
                return StatusCode(response.StatusCode, response);
            }
            return BadRequest(ModelState);
        }
        [HttpPut]
        [Route("{id}")]
        public async Task<ActionResult> Update(int id, PurchaseCommandDto Purchase)
        {
            if (ModelState.IsValid)
            {
                var response = await _mediator.Send(new UpdatePurchaseCommand { Id = id, PurchaseDto = Purchase });
                return StatusCode(response.StatusCode, response);
            }
            return BadRequest(ModelState);
        }
        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var response = await _mediator.Send(new DeletePurchaseCommand { Id = id });
            return StatusCode(response.StatusCode, response);
        }

    }
}
