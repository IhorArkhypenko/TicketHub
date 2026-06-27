using BuildingBlocks.Observability.HealthChecks;
using Booking.Application.Abstractions;
using Booking.Infrastructure.Grpc;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Saga;
using Contracts.Protos.Catalog;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string postgres = configuration.GetConnectionString("BookingDb")
            ?? throw new InvalidOperationException("Connection string 'BookingDb' is not configured.");

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(postgres, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BookingDbContext.Schema)));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, BookingUnitOfWork>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<ISeatAvailabilityChecker, CatalogSeatChecker>();

        // Saga activities (resolved per-message scope by the state machine).
        services.AddScoped<ApplySeatReservedActivity>();
        services.AddScoped<ConfirmBookingActivity>();
        services.AddScoped<CancelOnPaymentFailedActivity>();
        services.AddScoped<CancelOnTimeoutActivity>();
        services.AddScoped<RejectBookingActivity>();

        string grpcAddress = configuration["Catalog:GrpcAddress"] ?? "http://localhost:5111";
        services.AddGrpcClient<CatalogSeatCheck.CatalogSeatCheckClient>(o => o.Address = new Uri(grpcAddress))
            // Retry (transient), per-attempt timeout and a circuit breaker on the synchronous
            // Catalog dependency, so a flaky/slow Catalog doesn't cascade into Booking.
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

        AddBookingMessaging(services, configuration);

        services.AddTicketHubHealthChecks()
            .AddNpgSql(postgres, name: "postgres", tags: new[] { HealthCheckExtensions.ReadyTag });

        return services;
    }

    private static void AddBookingMessaging(IServiceCollection services, IConfiguration configuration)
    {
        string host = configuration["RabbitMq:Host"] ?? "localhost";
        ushort port = ushort.TryParse(configuration["RabbitMq:Port"], out ushort p) ? p : (ushort)5672;
        string username = configuration["RabbitMq:Username"] ?? "tickethub";
        string password = configuration["RabbitMq:Password"] ?? "tickethub";

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddDelayedMessageScheduler();

            bus.AddEntityFrameworkOutbox<BookingDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.AddSagaStateMachine<BookingStateMachine, BookingState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<BookingDbContext>();
                    r.UsePostgres();
                });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, port, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Hold-timeout scheduling via the RabbitMQ delayed-message exchange plugin
                // (the masstransit/rabbitmq image bundles it).
                cfg.UseDelayedMessageScheduler();
                cfg.UseMessageRetry(r => r.Immediate(2));
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
