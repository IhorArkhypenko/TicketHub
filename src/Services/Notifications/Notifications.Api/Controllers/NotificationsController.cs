using Asp.Versioning;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Observability.Errors;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.GetUserNotifications;

namespace Notifications.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>Returns the notification history for a user.</summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetForUser(Guid userId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<NotificationDto>> result = await _sender.Send(new GetUserNotificationsQuery(userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error);
    }
}
