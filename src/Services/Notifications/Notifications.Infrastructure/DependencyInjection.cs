using BuildingBlocks.Observability.HealthChecks;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Messaging;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string mongo = configuration.GetConnectionString("Mongo")
            ?? throw new InvalidOperationException("Connection string 'Mongo' is not configured.");
        string databaseName = configuration["Mongo:Database"] ?? "notifications";

        MongoSetup.RegisterClassMaps();

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongo));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();
        services.AddSingleton<INotificationTemplates, NotificationTemplates>();
        services.AddHostedService<MongoIndexInitializer>();

        AddNotificationsMessaging(services, configuration);

        services.AddTicketHubHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo", tags: new[] { HealthCheckExtensions.ReadyTag });

        return services;
    }

    private static void AddNotificationsMessaging(IServiceCollection services, IConfiguration configuration)
    {
        string host = configuration["RabbitMq:Host"] ?? "localhost";
        ushort port = ushort.TryParse(configuration["RabbitMq:Port"], out ushort p) ? p : (ushort)5672;
        string username = configuration["RabbitMq:Username"] ?? "tickethub";
        string password = configuration["RabbitMq:Password"] ?? "tickethub";

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<BookingConfirmedConsumer>();
            bus.AddConsumer<BookingCancelledConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, port, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(r => r.Immediate(2));
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
