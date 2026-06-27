using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Booking.Domain.Events;

namespace Booking.Domain;

/// <summary>
/// Rich aggregate root for a booking. All state changes go through guarded methods that enforce
/// invariants (e.g. an unpaid booking cannot be confirmed; a confirmed booking cannot be
/// cancelled) and raise domain events — there are no public setters.
/// </summary>
public sealed class Booking : AggregateRoot<Guid>
{
    private Booking(Guid id, Guid userId, Guid sessionId, Guid seatId, decimal amount, string currency)
        : base(id)
    {
        UserId = userId;
        SessionId = sessionId;
        SeatId = seatId;
        Amount = amount;
        Currency = currency;
        Status = BookingStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new BookingSubmittedDomainEvent(id, userId, sessionId, seatId, amount, currency));
    }

    private Booking() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid SeatId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public BookingStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Booking Submit(Guid userId, Guid sessionId, Guid seatId, decimal amount, string currency)
        => new(Guid.NewGuid(), userId, sessionId, seatId, amount, currency);

    public Result MarkSeatReserved()
    {
        if (Status != BookingStatus.Pending)
        {
            return Result.Failure(BookingErrors.InvalidSeatReservation);
        }

        Status = BookingStatus.SeatReserved;
        RaiseDomainEvent(new BookingSeatReservedDomainEvent(Id));
        return Result.Success();
    }

    public Result RequestPayment()
    {
        if (Status != BookingStatus.SeatReserved)
        {
            return Result.Failure(BookingErrors.InvalidPaymentRequest);
        }

        Status = BookingStatus.AwaitingPayment;
        RaiseDomainEvent(new BookingAwaitingPaymentDomainEvent(Id));
        return Result.Success();
    }

    public Result Confirm()
    {
        // Invariant: a booking can only be confirmed once it is awaiting payment (i.e. paid).
        if (Status != BookingStatus.AwaitingPayment)
        {
            return Result.Failure(BookingErrors.CannotConfirmUnpaid);
        }

        Status = BookingStatus.Confirmed;
        RaiseDomainEvent(new BookingConfirmedDomainEvent(Id, UserId, SeatId));
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        // Invariant: a confirmed booking cannot be cancelled.
        if (Status == BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.CannotCancelConfirmed);
        }

        if (Status is BookingStatus.Cancelled or BookingStatus.Rejected)
        {
            return Result.Failure(BookingErrors.AlreadyTerminal);
        }

        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        RaiseDomainEvent(new BookingCancelledDomainEvent(Id, UserId, reason));
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status is not (BookingStatus.Pending or BookingStatus.SeatReserved))
        {
            return Result.Failure(BookingErrors.AlreadyTerminal);
        }

        Status = BookingStatus.Rejected;
        CancellationReason = reason;
        RaiseDomainEvent(new BookingRejectedDomainEvent(Id, UserId, reason));
        return Result.Success();
    }

    public bool IsTerminal => Status is BookingStatus.Confirmed or BookingStatus.Cancelled or BookingStatus.Rejected;
}
