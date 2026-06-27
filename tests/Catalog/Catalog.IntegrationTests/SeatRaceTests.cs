using System.Collections.Concurrent;
using System.Net.Http.Json;
using Contracts.Events.Catalog.V1;
using FluentAssertions;
using MassTransit;
using Xunit;

namespace Catalog.IntegrationTests;

public sealed class SeatRaceTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public SeatRaceTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record CreateEventRequest(string Title, string Description, string Venue, string Category);
    private sealed record SeatDef(string Row, int Number, decimal Price, string Currency);
    private sealed record AddSessionBody(DateTime StartsAtUtc, SeatDef[] Seats);
    private sealed record SeatView(Guid Id, Guid SessionId, string Row, int Number, decimal Price, string Currency, string Status);

    [Fact]
    public async Task Two_concurrent_reservations_for_one_seat_yield_exactly_one_winner()
    {
        // Arrange: an event with a single seat.
        var create = await _client.PostAsJsonAsync("/api/v1/events",
            new CreateEventRequest("Race", "d", "Arena", "Concert"));
        Guid eventId = await create.Content.ReadFromJsonAsync<Guid>();

        var sessionResponse = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/sessions",
            new AddSessionBody(DateTime.UtcNow.AddDays(5), new[] { new SeatDef("A", 1, 100m, "USD") }));
        Guid sessionId = await sessionResponse.Content.ReadFromJsonAsync<Guid>();

        var seats = await _client.GetFromJsonAsync<List<SeatView>>($"/api/v1/sessions/{sessionId}/seats");
        Guid seatId = seats!.Single().Id;

        var reserved = new ConcurrentBag<Guid>();
        var rejected = new ConcurrentBag<Guid>();
        var seen = new CountdownEvent(2);

        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(_factory.RabbitMqHost, (ushort)_factory.RabbitMqPort, "/", h =>
            {
                h.Username(CatalogApiFactory.RabbitMqUser);
                h.Password(CatalogApiFactory.RabbitMqPassword);
            });

            cfg.ReceiveEndpoint("seat-race-test", e =>
            {
                e.Handler<SeatReserved>(ctx =>
                {
                    if (ctx.Message.SeatId == seatId) { reserved.Add(ctx.Message.BookingId); seen.Signal(); }
                    return Task.CompletedTask;
                });
                e.Handler<SeatReservationRejected>(ctx =>
                {
                    if (ctx.Message.SeatId == seatId) { rejected.Add(ctx.Message.BookingId); seen.Signal(); }
                    return Task.CompletedTask;
                });
            });
        });

        await bus.StartAsync();
        try
        {
            // Act: two bookings race for the same seat.
            Guid bookingA = Guid.NewGuid();
            Guid bookingB = Guid.NewGuid();
            await Task.WhenAll(
                bus.Publish(new ReserveSeat(bookingA, seatId)),
                bus.Publish(new ReserveSeat(bookingB, seatId)));

            seen.Wait(TimeSpan.FromSeconds(30)).Should().BeTrue("both reservation attempts should be answered");

            // Assert: exactly one winner, no double booking.
            reserved.Should().HaveCount(1);
            rejected.Should().HaveCount(1);

            var finalSeats = await _client.GetFromJsonAsync<List<SeatView>>($"/api/v1/sessions/{sessionId}/seats");
            finalSeats!.Single().Status.Should().Be("Held");
        }
        finally
        {
            await bus.StopAsync();
        }
    }
}
