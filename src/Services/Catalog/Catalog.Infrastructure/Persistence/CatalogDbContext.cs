using Catalog.Domain.Events;
using Catalog.Domain.Seats;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public const string Schema = "catalog";

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
