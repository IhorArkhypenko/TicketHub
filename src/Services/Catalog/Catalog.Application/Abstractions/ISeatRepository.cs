using Catalog.Domain.Seats;

namespace Catalog.Application.Abstractions;

public interface ISeatRepository
{
    void AddRange(IEnumerable<Seat> seats);

    Task<Seat?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Seat>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken);
}
