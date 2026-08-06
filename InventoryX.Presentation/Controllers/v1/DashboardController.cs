using InventoryX.Application.Queries.Requests.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1/dashboard")]
[Authorize(Roles = "Owner,Administrator,Manager,Accountant")]
public sealed class DashboardController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get(DateTime? asOf, CancellationToken cancellationToken) =>
        sender.Send(new GetDashboardQuery(asOf, User.IsInRole("Owner") || User.IsInRole("Administrator") || User.IsInRole("Manager")), cancellationToken);
}
