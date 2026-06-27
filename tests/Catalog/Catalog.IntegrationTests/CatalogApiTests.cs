using System.Net;
using System.Net.Http.Json;
using Contracts.Protos.Catalog;
using FluentAssertions;
using Grpc.Net.Client;
using Xunit;

namespace Catalog.IntegrationTests;

public sealed class CatalogApiTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public CatalogApiTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record CreateEventRequest(string Title, string Description, string Venue, string Category);
    private sealed record SeatDef(string Row, int Number, decimal Price, string Currency);
    private sealed record AddSessionBody(DateTime StartsAtUtc, SeatDef[] Seats);
    private sealed record SeatView(Guid Id, Guid SessionId, string Row, int Number, decimal Price, string Currency, string Status);

    private async Task<Guid> CreateEventAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/events",
            new CreateEventRequest("Coldplay", "Tour", "Arena", "Concert"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> AddSessionAsync(Guid eventId, params SeatDef[] seats)
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/sessions",
            new AddSessionBody(DateTime.UtcNow.AddDays(30), seats));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Create_then_list_event_roundtrips()
    {
        Guid id = await CreateEventAsync();

        var events = await _client.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/v1/events");

        events.Should().NotBeNull();
        events!.Should().Contain(e => e["id"].ToString() == id.ToString());
    }

    [Fact]
    public async Task Adding_a_session_creates_available_seats()
    {
        Guid eventId = await CreateEventAsync();
        Guid sessionId = await AddSessionAsync(eventId,
            new SeatDef("A", 1, 100m, "USD"),
            new SeatDef("A", 2, 120m, "USD"));

        var seats = await _client.GetFromJsonAsync<List<SeatView>>($"/api/v1/sessions/{sessionId}/seats");

        seats.Should().HaveCount(2);
        seats!.Should().OnlyContain(s => s.Status == "Available");
        seats!.Should().Contain(s => s.Row == "A" && s.Number == 1 && s.Price == 100m);
    }

    [Fact]
    public async Task Grpc_CheckSeat_returns_availability_and_price()
    {
        Guid eventId = await CreateEventAsync();
        Guid sessionId = await AddSessionAsync(eventId, new SeatDef("B", 5, 75m, "EUR"));

        var seats = await _client.GetFromJsonAsync<List<SeatView>>($"/api/v1/sessions/{sessionId}/seats");
        Guid seatId = seats!.Single().Id;

        using GrpcChannel channel = GrpcChannel.ForAddress(_client.BaseAddress!,
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });
        var grpc = new CatalogSeatCheck.CatalogSeatCheckClient(channel);

        CheckSeatReply reply = await grpc.CheckSeatAsync(new CheckSeatRequest { SeatId = seatId.ToString() });

        reply.Exists.Should().BeTrue();
        reply.Available.Should().BeTrue();
        reply.Amount.Should().Be(75d);
        reply.Currency.Should().Be("EUR");
        reply.SessionId.Should().Be(sessionId.ToString());
    }

    [Fact]
    public async Task Grpc_CheckSeat_for_unknown_seat_reports_not_existing()
    {
        using GrpcChannel channel = GrpcChannel.ForAddress(_client.BaseAddress!,
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });
        var grpc = new CatalogSeatCheck.CatalogSeatCheckClient(channel);

        CheckSeatReply reply = await grpc.CheckSeatAsync(new CheckSeatRequest { SeatId = Guid.NewGuid().ToString() });

        reply.Exists.Should().BeFalse();
        reply.Available.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_create_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/events",
            new CreateEventRequest("", "d", "", "Nope"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task Event_list_cache_is_invalidated_on_write()
    {
        // Populate the cache, then write — the second read must reflect the new event,
        // proving the cache-aside entry was invalidated rather than served stale.
        await CreateEventAsync();
        await _client.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/v1/events");

        Guid freshId = await CreateEventAsync();

        var afterWrite = await _client.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/v1/events");
        afterWrite!.Should().Contain(e => e["id"].ToString() == freshId.ToString());
    }

    [Fact]
    public async Task Unknown_event_returns_not_found()
    {
        var response = await _client.GetAsync($"/api/v1/events/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
