using Booking.Infrastructure.Saga;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.Infrastructure.Persistence;

public sealed class BookingDbContext : DbContext
{
    public const string Schema = "booking";

    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<BookingAggregate> Bookings => Set<BookingAggregate>();
    public DbSet<BookingState> BookingStates => Set<BookingState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddTransactionalOutboxEntities();
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new BookingStateConfiguration());
    }
}

internal sealed class BookingConfiguration : IEntityTypeConfiguration<BookingAggregate>
{
    public void Configure(EntityTypeBuilder<BookingAggregate> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Ignore(b => b.DomainEvents);

        builder.Property(b => b.UserId).IsRequired();
        builder.Property(b => b.SessionId).IsRequired();
        builder.Property(b => b.SeatId).IsRequired();
        builder.Property(b => b.Amount).HasColumnType("numeric(18,2)");
        builder.Property(b => b.Currency).HasMaxLength(3);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.CancellationReason).HasMaxLength(500);
        builder.Property(b => b.CreatedAtUtc);
    }
}

internal sealed class BookingStateConfiguration : IEntityTypeConfiguration<BookingState>
{
    public void Configure(EntityTypeBuilder<BookingState> builder)
    {
        builder.ToTable("booking_states");
        builder.HasKey(x => x.CorrelationId);
        builder.Property(x => x.CorrelationId).ValueGeneratedNever();
        builder.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        // Optimistic concurrency for the saga instance.
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
