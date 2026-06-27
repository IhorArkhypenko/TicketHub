using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Messaging;

public static class MassTransitConfiguration
{
    /// <summary>
    /// Wires MassTransit over RabbitMQ with the EF Core Transactional Outbox (producer side:
    /// messages are saved in the same transaction as state and delivered after commit, so they
    /// are never lost) and Inbox (consumer side: dedup by messageId, so at-least-once redelivery
    /// has no double effect). Consumers are registered via <paramref name="configureConsumers"/>.
    /// </summary>
    public static IServiceCollection AddTicketHubMassTransit<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null,
        params Assembly[] consumerAssemblies)
        where TDbContext : DbContext
    {
        string host = configuration["RabbitMq:Host"] ?? "localhost";
        ushort port = ushort.TryParse(configuration["RabbitMq:Port"], out ushort p) ? p : (ushort)5672;
        string username = configuration["RabbitMq:Username"] ?? "tickethub";
        string password = configuration["RabbitMq:Password"] ?? "tickethub";
        string virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
            });

            if (consumerAssemblies.Length > 0)
            {
                bus.AddConsumers(consumerAssemblies);
            }

            configureConsumers?.Invoke(bus);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, port, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(r => r.Immediate(2));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
