using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Observability.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string postgres = configuration.GetConnectionString("PaymentDb")
            ?? throw new InvalidOperationException("Connection string 'PaymentDb' is not configured.");

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(postgres, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PaymentDbContext.Schema)));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddSingleton<IPaymentSimulator, PaymentSimulator>();

        services.AddTicketHubMassTransit<PaymentDbContext>(
            configuration,
            consumerAssemblies: typeof(ProcessPaymentConsumer).Assembly);

        services.AddTicketHubHealthChecks()
            .AddNpgSql(postgres, name: "postgres", tags: new[] { HealthCheckExtensions.ReadyTag });

        return services;
    }
}
