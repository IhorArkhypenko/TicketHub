namespace Contracts.Events.Booking.V1;

// Event that starts the booking Saga.
public sealed record BookingSubmitted(
    Guid BookingId,
    Guid UserId,
    Guid SessionId,
    Guid SeatId,
    decimal Amount,
    string Currency);

// Terminal booking events (consumed by Notifications and any interested service).
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid UserId,
    Guid SeatId,
    DateTime OccurredOnUtc);

public sealed record BookingCancelled(
    Guid BookingId,
    Guid UserId,
    string Reason,
    DateTime OccurredOnUtc);

public sealed record BookingRejected(
    Guid BookingId,
    Guid UserId,
    string Reason,
    DateTime OccurredOnUtc);
