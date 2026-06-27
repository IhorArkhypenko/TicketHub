using Catalog.Application.Abstractions;

namespace Catalog.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
