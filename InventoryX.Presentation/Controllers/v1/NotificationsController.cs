using InventoryX.Application.Commands.Requests.Notifications;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.Notifications;
using InventoryX.Application.Queries.Requests.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryX.Presentation.Controllers.v1;

[Route("api/v1")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ApiControllerBase
{
    [HttpGet("notifications")]
    public Task<PagedResult<NotificationDto>> Feed([FromQuery] GetNotificationsQuery query, CancellationToken cancellationToken) =>
        sender.Send(query, cancellationToken);

    [HttpPost("notifications/{id:guid}/read")]
    public Task<bool> Read(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new MarkNotificationReadCommand(id), cancellationToken);

    [HttpPost("notifications/read-all")]
    public Task<int> ReadAll(CancellationToken cancellationToken) =>
        sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);

    [HttpGet("notification-preferences")]
    public Task<IReadOnlyList<NotificationPreferenceDto>> Preferences(CancellationToken cancellationToken) =>
        sender.Send(new GetNotificationPreferencesQuery(), cancellationToken);

    [HttpPut("notification-preferences")]
    public Task<IReadOnlyList<NotificationPreferenceDto>> UpdatePreferences(
        UpdateNotificationPreferencesCommand command, CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);
}
