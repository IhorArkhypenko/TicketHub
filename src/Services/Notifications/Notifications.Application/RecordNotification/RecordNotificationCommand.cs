using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Notifications.Application.Abstractions;
using Notifications.Domain;

namespace Notifications.Application.RecordNotification;

public sealed record RecordNotificationCommand(Guid UserId, Guid BookingId, NotificationType Type, string? Reason) : ICommand;

internal sealed class RecordNotificationCommandHandler : ICommandHandler<RecordNotificationCommand>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationSender _sender;
    private readonly INotificationTemplates _templates;

    public RecordNotificationCommandHandler(
        INotificationRepository repository,
        INotificationSender sender,
        INotificationTemplates templates)
    {
        _repository = repository;
        _sender = sender;
        _templates = templates;
    }

    public async Task<Result> Handle(RecordNotificationCommand request, CancellationToken cancellationToken)
    {
        (string subject, string body) = _templates.Render(request.Type, request.BookingId, request.Reason);
        Notification notification = Notification.Create(request.UserId, request.BookingId, request.Type, subject, body);

        // Idempotent: only send when this (booking, type) notification is newly stored.
        bool added = await _repository.AddIfNotExistsAsync(notification, cancellationToken);
        if (added)
        {
            await _sender.SendAsync(notification, cancellationToken);
        }

        return Result.Success();
    }
}
