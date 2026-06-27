using BuildingBlocks.Domain.Primitives;
using Booking.Application.Abstractions;
using Booking.Application.Observability;
using Booking.Domain.Events;
using Contracts.Events.Booking.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.Infrastructure.Persistence;

internal sealed class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context) => _context = context;

    public Task<BookingAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task AddAsync(BookingAggregate booking, CancellationToken cancellationToken)
        => await _context.Bookings.AddAsync(booking, cancellationToken);
}

/// <summary>
/// Saves changes and dispatches the aggregate's terminal domain events as integration events
/// through the outbox (transactional with the state change), mapping rich domain events to the
/// versioned contracts consumed by other services.
/// </summary>
internal sealed class BookingUnitOfWork : IUnitOfWork
{
    private readonly BookingDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly BookingMetrics _metrics;

    public BookingUnitOfWork(BookingDbContext context, IPublishEndpoint publishEndpoint, BookingMetrics metrics)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _metrics = metrics;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        List<IDomainEvent> domainEvents = _context.ChangeTracker
            .Entries<BookingAggregate>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            object? integrationEvent = Map(domainEvent);
            if (integrationEvent is not null)
            {
                await _publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
            }

            RecordMetric(domainEvent);
        }

        foreach (var entry in _context.ChangeTracker.Entries<BookingAggregate>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    private static object? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        BookingConfirmedDomainEvent e => new BookingConfirmed(e.BookingId, e.UserId, e.SeatId, e.OccurredOnUtc),
        BookingCancelledDomainEvent e => new BookingCancelled(e.BookingId, e.UserId, e.Reason, e.OccurredOnUtc),
        BookingRejectedDomainEvent e => new BookingRejected(e.BookingId, e.UserId, e.Reason, e.OccurredOnUtc),
        _ => null
    };

    private void RecordMetric(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case BookingConfirmedDomainEvent: _metrics.Confirmed(); break;
            case BookingCancelledDomainEvent: _metrics.Cancelled(); break;
            case BookingRejectedDomainEvent: _metrics.Rejected(); break;
        }
    }
}
