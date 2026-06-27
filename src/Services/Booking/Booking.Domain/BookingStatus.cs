namespace Booking.Domain;

/// <summary>
/// Booking lifecycle: Pending → SeatReserved → AwaitingPayment → Confirmed,
/// with Cancelled / Rejected as terminal compensation outcomes.
/// </summary>
public enum BookingStatus
{
    Pending = 0,
    SeatReserved = 1,
    AwaitingPayment = 2,
    Confirmed = 3,
    Cancelled = 4,
    Rejected = 5
}
