using Catalog.Application.Abstractions;
using Catalog.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

internal sealed class EventRepository : IEventRepository
{
    private readonly CatalogDbContext _context;

    public EventRepository(CatalogDbContext context) => _context = context;

    public async Task AddAsync(Event @event, CancellationToken cancellationToken)
        => await _context.Events.AddAsync(@event, cancellationToken);

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _context.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Event>> ListAsync(CancellationToken cancellationToken)
        => await _context.Events
            .Include(e => e.Sessions)
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken)
        => await _context.Sessions.AnyAsync(s => s.Id == sessionId, cancellationToken);
}
