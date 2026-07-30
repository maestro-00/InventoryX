using InventoryX.Application.Commands.Requests.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ApiControllerBase
{
    public sealed record SetPinRequest(string Pin);

    [HttpPut("{userId}/pin")]
    public async Task<IActionResult> SetPin(string userId, SetPinRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetRegisterPinCommand { UserId = userId, Pin = request.Pin }, cancellationToken);
        return NoContent();
    }
}
