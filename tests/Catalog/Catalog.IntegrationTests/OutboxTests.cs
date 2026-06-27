using System.Net.Http.Json;
using Contracts.Events.Catalog.V1;
using FluentAssertions;
using MassTransit;
using Xunit;

namespace Catalog.IntegrationTests;

public sealed class OutboxTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public OutboxTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record CreateEventRequest(string Title, string Description, string Venue, string Category);
    private sealed record SeatDef(string Row, int Number, decimal Price, string Currency);
    private sealed record AddSessionBody(DateTime StartsAtUtc, SeatDef[] Seats);

    [Fact]
    public async Task Adding_a_session_publishes_SessionScheduled_through_the_outbox()
    {
        // A standalone consumer bus on the same broker stands in for downstream services.
        var received = new TaskCompletionSource<SessionScheduled>(TaskCreationOptions.RunContinuationsAsynchronously);

        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(_factory.RabbitMqHost, (ushort)_factory.RabbitMqPort, "/", h =>
            {
                h.Username(CatalogApiFactory.RabbitMqUser);
                h.Password(CatalogApiFactory.RabbitMqPassword);
            });

            cfg.ReceiveEndpoint("catalog-outbox-test", e =>
                e.Handler<SessionScheduled>(context =>
                {
                    received.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
        });

        await bus.StartAsync();
        try
        {
            var createResponse = await _client.PostAsJsonAsync("/api/v1/events",
                new CreateEventRequest("Outbox Concert", "d", "Arena", "Concert"));
            Guid eventId = await createResponse.Content.ReadFromJsonAsync<Guid>();

            var sessionResponse = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/sessions",
                new AddSessionBody(DateTime.UtcNow.AddDays(10), new[] { new SeatDef("A", 1, 50m, "USD") }));
            Guid sessionId = await sessionResponse.Content.ReadFromJsonAsync<Guid>();

            Task completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(30)));

            completed.Should().Be(received.Task, "the outbox should dispatch SessionScheduled to the broker");
            received.Task.Result.SessionId.Should().Be(sessionId);
            received.Task.Result.SeatCount.Should().Be(1);
        }
        finally
        {
            await bus.StopAsync();
        }
    }
}
