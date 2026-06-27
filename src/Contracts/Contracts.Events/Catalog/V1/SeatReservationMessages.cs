namespace Contracts.Events.Catalog.V1;

// Saga commands consumed by Catalog (correlated to a booking) and their reply events.
// Distinct from the broadcast SeatHeld/SeatReleased/SeatSold notifications, these carry the
// BookingId so the Saga can correlate the response.

public sealed record ReserveSeat(Guid BookingId, Guid SeatId);

public sealed record ConfirmSeat(Guid BookingId, Guid SeatId);

public sealed record ReleaseSeat(Guid BookingId, Guid SeatId);

public sealed record SeatReserved(Guid BookingId, Guid SeatId, DateTime OccurredOnUtc);

public sealed record SeatReservationRejected(Guid BookingId, Guid SeatId, string Reason, DateTime OccurredOnUtc);
