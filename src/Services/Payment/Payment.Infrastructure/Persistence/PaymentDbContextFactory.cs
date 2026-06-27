using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payment.Infrastructure.Persistence;

/// <summary>Design-time factory so `dotnet ef` can scaffold migrations without booting the host.</summary>
public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=payment;Username=tickethub;Password=tickethub",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", PaymentDbContext.Schema))
            .Options;

        return new PaymentDbContext(options);
    }
}
