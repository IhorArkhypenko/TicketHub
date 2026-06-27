using Catalog.Application.Abstractions;
using Catalog.Domain.Seats;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

internal sealed class SeatRepository : ISeatRepository
{
    private readonly CatalogDbContext _context;

    public SeatRepository(CatalogDbContext context) => _context = context;

    public void AddRange(IEnumerable<Seat> seats) => _context.Seats.AddRange(seats);

    public async Task<Seat?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _context.Seats.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Seat>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => await _context.Seats
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .OrderBy(s => s.Number.Row)
            .ThenBy(s => s.Number.Number)
            .ToListAsync(cancellationToken);
}
