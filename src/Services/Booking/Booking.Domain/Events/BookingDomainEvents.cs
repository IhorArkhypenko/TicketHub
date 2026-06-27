using BuildingBlocks.Domain.Primitives;

namespace Booking.Domain.Events;

public sealed record BookingSubmittedDomainEvent(
    Guid BookingId, Guid UserId, Guid SessionId, Guid SeatId, decimal Amount, string Currency) : DomainEvent;

public sealed record BookingSeatReservedDomainEvent(Guid BookingId) : DomainEvent;

public sealed record BookingAwaitingPaymentDomainEvent(Guid BookingId) : DomainEvent;

public sealed record BookingConfirmedDomainEvent(Guid BookingId, Guid UserId, Guid SeatId) : DomainEvent;

public sealed record BookingCancelledDomainEvent(Guid BookingId, Guid UserId, string Reason) : DomainEvent;

public sealed record BookingRejectedDomainEvent(Guid BookingId, Guid UserId, string Reason) : DomainEvent;
