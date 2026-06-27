using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Notifications.Application.Abstractions;
using Notifications.Domain;

namespace Notifications.Application.GetUserNotifications;

public sealed record NotificationDto(Guid Id, Guid BookingId, string Type, string Subject, string Body, DateTime CreatedAtUtc);

public sealed record GetUserNotificationsQuery(Guid UserId) : IQuery<IReadOnlyList<NotificationDto>>;

internal sealed class GetUserNotificationsQueryHandler : IQueryHandler<GetUserNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repository;

    public GetUserNotificationsQueryHandler(INotificationRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        GetUserNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Notification> notifications = await _repository.ListByUserAsync(request.UserId, cancellationToken);

        IReadOnlyList<NotificationDto> dtos = notifications
            .Select(n => new NotificationDto(n.Id, n.BookingId, n.Type.ToString(), n.Subject, n.Body, n.CreatedAtUtc))
            .ToList();

        return Result.Success(dtos);
    }
}
