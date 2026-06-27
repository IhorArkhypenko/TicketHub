using Notifications.Domain;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository
{
    /// <summary>Inserts the notification; returns false if one already exists for the same booking
    /// and type (idempotent against redelivery, deduped by business key).</summary>
    Task<bool> AddIfNotExistsAsync(Notification notification, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>"Delivers" a notification — here, by structured logging.</summary>
public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken);
}

/// <summary>Renders the subject/body for a notification type from a template.</summary>
public interface INotificationTemplates
{
    (string Subject, string Body) Render(NotificationType type, Guid bookingId, string? reason);
}
