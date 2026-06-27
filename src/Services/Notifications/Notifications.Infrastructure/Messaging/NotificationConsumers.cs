using BuildingBlocks.Application.Messaging;
using Contracts.Events.Booking.V1;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Application.RecordNotification;
using Notifications.Domain;

namespace Notifications.Infrastructure.Messaging;

public sealed class BookingConfirmedConsumer : IConsumer<BookingConfirmed>
{
    private readonly ISender _sender;

    public BookingConfirmedConsumer(ISender sender) => _sender = sender;

    public Task Consume(ConsumeContext<BookingConfirmed> context)
        => _sender.Send(
            new RecordNotificationCommand(context.Message.UserId, context.Message.BookingId, NotificationType.BookingConfirmed, null),
            context.CancellationToken);
}

public sealed class BookingCancelledConsumer : IConsumer<BookingCancelled>
{
    private readonly ISender _sender;

    public BookingCancelledConsumer(ISender sender) => _sender = sender;

    public Task Consume(ConsumeContext<BookingCancelled> context)
        => _sender.Send(
            new RecordNotificationCommand(context.Message.UserId, context.Message.BookingId, NotificationType.BookingCancelled, context.Message.Reason),
            context.CancellationToken);
}

/// <summary>Renders subject/body for each notification type from simple templates.</summary>
internal sealed class NotificationTemplates : INotificationTemplates
{
    public (string Subject, string Body) Render(NotificationType type, Guid bookingId, string? reason) => type switch
    {
        NotificationType.BookingConfirmed =>
            ("Your booking is confirmed",
             $"Good news! Your booking {bookingId} is confirmed and your ticket has been issued."),
        NotificationType.BookingCancelled =>
            ("Your booking was cancelled",
             $"Your booking {bookingId} was cancelled. Reason: {reason ?? "unspecified"}."),
        _ => ("Booking update", $"There is an update on your booking {bookingId}.")
    };
}

/// <summary>"Sends" the notification by structured logging (provider simulation).</summary>
internal sealed class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) => _logger = logger;

    public Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sent {Type} notification to user {UserId} for booking {BookingId}: {Subject}",
            notification.Type, notification.UserId, notification.BookingId, notification.Subject);
        return Task.CompletedTask;
    }
}
