using Catalog.Domain.Events;

namespace Catalog.Application.Abstractions;

public interface IEventRepository
{
    Task AddAsync(Event @event, CancellationToken cancellationToken);

    /// <summary>Loads an event including its sessions, or null if missing.</summary>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Event>> ListAsync(CancellationToken cancellationToken);

    Task<bool> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken);
}
