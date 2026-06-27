using Catalog.Domain.Seats;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Event = Catalog.Domain.Events.Event;
using Session = Catalog.Domain.Events.Session;

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

        // Transactional Outbox/Inbox tables (MassTransit): OutboxMessage, OutboxState, InboxState.
        modelBuilder.AddTransactionalOutboxEntities();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
