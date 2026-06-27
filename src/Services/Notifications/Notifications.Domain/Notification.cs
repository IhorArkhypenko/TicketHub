namespace Notifications.Domain;

public enum NotificationType
{
    BookingConfirmed = 0,
    BookingCancelled = 1
}

/// <summary>
/// A delivered notification, stored as a document. The domain is intentionally thin (DDD-lite):
/// notifications are semi-structured records with a flexible schema — an honest fit for MongoDB.
/// </summary>
public sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookingId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Channel { get; private set; } = "Log";
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { } // serialization

    public static Notification Create(Guid userId, Guid bookingId, NotificationType type, string subject, string body)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BookingId = bookingId,
            Type = type,
            Channel = "Log",
            Subject = subject,
            Body = body,
            CreatedAtUtc = DateTime.UtcNow
        };
}
