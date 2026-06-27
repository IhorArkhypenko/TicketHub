using Catalog.Application.Abstractions;
using MassTransit;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events via the scoped <see cref="IPublishEndpoint"/>. With the bus
/// outbox enabled, the message is buffered into the outbox table and committed together with
/// the DbContext SaveChanges, then delivered by the outbox dispatcher.
/// </summary>
internal sealed class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
        => _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
