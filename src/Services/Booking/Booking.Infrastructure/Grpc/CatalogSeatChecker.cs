using Booking.Application.Abstractions;
using Contracts.Protos.Catalog;
using MassTransit;

namespace Booking.Infrastructure.Grpc;

/// <summary>gRPC client adapter for the synchronous Catalog seat pre-check.</summary>
internal sealed class CatalogSeatChecker : ISeatAvailabilityChecker
{
    private readonly CatalogSeatCheck.CatalogSeatCheckClient _client;

    public CatalogSeatChecker(CatalogSeatCheck.CatalogSeatCheckClient client) => _client = client;

    public async Task<SeatAvailability> CheckAsync(Guid seatId, CancellationToken cancellationToken)
    {
        CheckSeatReply reply = await _client.CheckSeatAsync(
            new CheckSeatRequest { SeatId = seatId.ToString() },
            cancellationToken: cancellationToken);

        Guid sessionId = Guid.TryParse(reply.SessionId, out Guid parsed) ? parsed : Guid.Empty;
        return new SeatAvailability(reply.Exists, reply.Available, sessionId, (decimal)reply.Amount, reply.Currency);
    }
}

internal sealed class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
        => _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
