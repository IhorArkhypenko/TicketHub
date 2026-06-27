using Catalog.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Ignore(e => e.DomainEvents);

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Venue).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CreatedAtUtc);

        // Sessions are part of the Event aggregate, accessed through the _sessions backing field.
        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Event.Sessions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
