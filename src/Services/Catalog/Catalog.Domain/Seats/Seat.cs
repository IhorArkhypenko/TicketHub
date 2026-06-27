using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Catalog.Domain.Seats.Events;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Seats;

/// <summary>
/// A seat on a session and the source of truth for its status. Modelled as its own
/// aggregate root because the status is the contended resource (held/released/sold) and
/// changes independently of the afisha. Transitions go only through guarded methods.
/// </summary>
public sealed class Seat : AggregateRoot<Guid>
{
    private Seat(Guid id, Guid sessionId, SeatNumber number, Money price) : base(id)
    {
        SessionId = sessionId;
        Number = number;
        Price = price;
        Status = SeatStatus.Available;
    }

    private Seat() { } // EF Core

    public Guid SessionId { get; private set; }
    public SeatNumber Number { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public SeatStatus Status { get; private set; }

    public static Seat Create(Guid sessionId, SeatNumber number, Money price)
        => new(Guid.NewGuid(), sessionId, number, price);

    /// <summary>Move Available -> Held. Used when a booking reserves the seat.</summary>
    public Result Hold()
    {
        if (Status != SeatStatus.Available)
        {
            return Result.Failure(CatalogErrors.SeatNotAvailable);
        }

        Status = SeatStatus.Held;
        RaiseDomainEvent(new SeatHeldDomainEvent(Id, SessionId));
        return Result.Success();
    }

    /// <summary>Move Held -> Available. Compensation when payment fails or hold times out.</summary>
    public Result Release()
    {
        if (Status != SeatStatus.Held)
        {
            return Result.Failure(CatalogErrors.SeatNotHeld);
        }

        Status = SeatStatus.Available;
        RaiseDomainEvent(new SeatReleasedDomainEvent(Id, SessionId));
        return Result.Success();
    }

    /// <summary>Move Held -> Sold. Confirms the seat after a successful payment.</summary>
    public Result MarkSold()
    {
        if (Status != SeatStatus.Held)
        {
            return Result.Failure(CatalogErrors.SeatNotHeld);
        }

        Status = SeatStatus.Sold;
        RaiseDomainEvent(new SeatSoldDomainEvent(Id, SessionId));
        return Result.Success();
    }

    public bool IsAvailable => Status == SeatStatus.Available;
}
