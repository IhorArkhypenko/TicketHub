using System.Collections.Concurrent;
using Booking.Application.Abstractions;
using Booking.Domain.Events;
using BuildingBlocks.Domain.Primitives;
using Contracts.Events.Booking.V1;
using Contracts.Events.Catalog.V1;
using Contracts.Events.Payment.V1;
using MassTransit;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.SagaTests;

/// <summary>In-memory store for Booking aggregates, shared between the test and the saga activities.</summary>
public sealed class InMemoryBookingRepository : IBookingRepository
{
    public ConcurrentDictionary<Guid, BookingAggregate> Store { get; } = new();

    public Task<BookingAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Store.GetValueOrDefault(id));

    public Task AddAsync(BookingAggregate booking, CancellationToken cancellationToken)
    {
        Store[booking.Id] = booking;
        return Task.CompletedTask;
    }
}

/// <summary>Publishes the aggregate's terminal domain events as integration events (no EF).</summary>
public sealed class TestUnitOfWork : IUnitOfWork
{
    private readonly InMemoryBookingRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public TestUnitOfWork(InMemoryBookingRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (BookingAggregate booking in _repository.Store.Values)
        {
            foreach (IDomainEvent domainEvent in booking.DomainEvents.ToList())
            {
                object? integrationEvent = Map(domainEvent);
                if (integrationEvent is not null)
                {
                    await _publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
                }
            }
            booking.ClearDomainEvents();
        }
        return 1;
    }

    private static object? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        BookingConfirmedDomainEvent e => new BookingConfirmed(e.BookingId, e.UserId, e.SeatId, e.OccurredOnUtc),
        BookingCancelledDomainEvent e => new BookingCancelled(e.BookingId, e.UserId, e.Reason, e.OccurredOnUtc),
        BookingRejectedDomainEvent e => new BookingRejected(e.BookingId, e.UserId, e.Reason, e.OccurredOnUtc),
        _ => null
    };
}

/// <summary>
/// Stands in for Catalog. Rejects the reservation for a seat marked "taken", otherwise reserves.
/// Confirm/Release are recorded so tests can assert compensation happened.
/// </summary>
public sealed class StubCatalogConsumer :
    IConsumer<ReserveSeat>, IConsumer<ConfirmSeat>, IConsumer<ReleaseSeat>
{
    public static Guid TakenSeatId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly ConcurrentBag<Guid> Released = new();
    public static readonly ConcurrentBag<Guid> Confirmed = new();

    public Task Consume(ConsumeContext<ReserveSeat> context)
        => context.Message.SeatId == TakenSeatId
            ? context.Publish(new SeatReservationRejected(context.Message.BookingId, context.Message.SeatId, "Seat already taken.", DateTime.UtcNow))
            : context.Publish(new SeatReserved(context.Message.BookingId, context.Message.SeatId, DateTime.UtcNow));

    public Task Consume(ConsumeContext<ConfirmSeat> context)
    {
        Confirmed.Add(context.Message.BookingId);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<ReleaseSeat> context)
    {
        Released.Add(context.Message.BookingId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Stands in for Payment. Declines amount 13.13, ignores the magic "no-reply" amount (to exercise
/// the hold timeout), otherwise completes.
/// </summary>
public sealed class StubPaymentConsumer : IConsumer<ProcessPayment>
{
    public const decimal DeclineAmount = 13.13m;
    public const decimal NoReplyAmount = 99.99m;

    public Task Consume(ConsumeContext<ProcessPayment> context)
    {
        if (context.Message.Amount == NoReplyAmount)
        {
            return Task.CompletedTask; // never reply -> hold timeout fires
        }

        return context.Message.Amount == DeclineAmount
            ? context.Publish(new PaymentFailed(context.Message.BookingId, "Card declined by provider.", DateTime.UtcNow))
            : context.Publish(new PaymentCompleted(context.Message.BookingId, Guid.NewGuid(), context.Message.Amount, context.Message.Currency, DateTime.UtcNow));
    }
}
