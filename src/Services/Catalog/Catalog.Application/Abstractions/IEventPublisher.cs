namespace Catalog.Application.Abstractions;

/// <summary>
/// Publishes integration events. Backed by the transactional outbox, so messages are saved in
/// the same database transaction as the state change and delivered after commit.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
