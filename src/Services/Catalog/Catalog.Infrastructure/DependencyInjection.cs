using BuildingBlocks.Observability.HealthChecks;
using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Caching;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string postgres = configuration.GetConnectionString("CatalogDb")
            ?? throw new InvalidOperationException("Connection string 'CatalogDb' is not configured.");
        string redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(postgres, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CatalogDbContext.Schema)));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISeatRepository, SeatRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(redis);
        services.AddSingleton(multiplexer);
        services.AddSingleton<ICatalogCache, RedisCatalogCache>();

        services.AddTicketHubHealthChecks()
            .AddNpgSql(postgres, name: "postgres", tags: new[] { HealthCheckExtensions.ReadyTag })
            .AddRedis(redis, name: "redis", tags: new[] { HealthCheckExtensions.ReadyTag });

        return services;
    }
}
