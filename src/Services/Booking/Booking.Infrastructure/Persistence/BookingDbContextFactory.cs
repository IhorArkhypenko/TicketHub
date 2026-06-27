using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Booking.Infrastructure.Persistence;

public sealed class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=booking;Username=tickethub;Password=tickethub",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", BookingDbContext.Schema))
            .Options;

        return new BookingDbContext(options);
    }
}
