using Catalog.Domain.Events;
using Catalog.Domain.Seats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("seats");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.SessionId).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // SeatNumber value object, flattened into columns.
        builder.OwnsOne(s => s.Number, number =>
        {
            number.Property(n => n.Row).HasColumnName("row").HasMaxLength(20).IsRequired();
            number.Property(n => n.Number).HasColumnName("number").IsRequired();
        });

        // Money value object, flattened into columns.
        builder.OwnsOne(s => s.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("price_amount").HasColumnType("numeric(18,2)").IsRequired();
            price.Property(p => p.Currency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
        });

        // Optimistic concurrency via PostgreSQL's xmin system column — protects seat-status
        // transitions against lost updates when users race for the same seat (Phase 5).
        builder.UseXminAsConcurrencyToken();

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.SessionId);
    }
}
