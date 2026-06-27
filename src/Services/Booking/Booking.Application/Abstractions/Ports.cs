using BookingAggregate = Booking.Domain.Booking;

namespace Booking.Application.Abstractions;

public interface IBookingRepository
{
    Task<BookingAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(BookingAggregate booking, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken) where TEvent : class;
}

/// <summary>Result of the synchronous gRPC seat pre-check against Catalog.</summary>
public sealed record SeatAvailability(bool Exists, bool Available, Guid SessionId, decimal Amount, string Currency);

/// <summary>Port over the gRPC call to Catalog, used to pre-validate a seat before starting the Saga.</summary>
public interface ISeatAvailabilityChecker
{
    Task<SeatAvailability> CheckAsync(Guid seatId, CancellationToken cancellationToken);
}
