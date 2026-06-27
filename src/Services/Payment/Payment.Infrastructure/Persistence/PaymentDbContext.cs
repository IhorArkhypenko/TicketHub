using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext : DbContext
{
    public const string Schema = "payment";

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddTransactionalOutboxEntities();
        modelBuilder.ApplyConfiguration(new PaymentRecordConfiguration());
    }
}

internal sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.BookingId).IsRequired();
        // One payment per booking — the idempotency guarantee at the database level.
        builder.HasIndex(p => p.BookingId).IsUnique();

        builder.Property(p => p.Amount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.Property(p => p.CreatedAtUtc);
        builder.Property(p => p.ProcessedAtUtc);
    }
}
