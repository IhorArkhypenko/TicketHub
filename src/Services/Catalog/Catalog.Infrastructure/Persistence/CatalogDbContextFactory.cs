using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef` so migrations can be created without booting the
/// web host. The connection string here is only used at design time.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=catalog;Username=tickethub;Password=tickethub",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", CatalogDbContext.Schema));

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
