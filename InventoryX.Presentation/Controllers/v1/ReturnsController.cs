using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/returns")]
[Authorize]
public sealed class ReturnsController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReturnTransactionDto>> Create(
        CreateReturnCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("exchange")]
    public async Task<ActionResult<ReturnTransactionDto>> Exchange(
        CreateExchangeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
