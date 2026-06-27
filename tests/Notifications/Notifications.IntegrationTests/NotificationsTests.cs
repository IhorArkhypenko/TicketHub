using System.Net.Http.Json;
using Contracts.Events.Booking.V1;
using FluentAssertions;
using MassTransit;
using Xunit;

namespace Notifications.IntegrationTests;

public sealed class NotificationsTests : IClassFixture<NotificationsApiFactory>, IAsyncLifetime
{
    private readonly NotificationsApiFactory _factory;
    private readonly HttpClient _client;
    private IBusControl _bus = null!;

    public NotificationsTests(NotificationsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        _ = _factory.Services; // start host (consumers + index init)
        _bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            cfg.Host(_factory.RabbitMqHost, (ushort)_factory.RabbitMqPort, "/", h =>
            {
                h.Username(NotificationsApiFactory.RabbitMqUser);
                h.Password(NotificationsApiFactory.RabbitMqPassword);
            }));
        await _bus.StartAsync();
    }

    public async Task DisposeAsync() => await _bus.StopAsync();

    private sealed record NotificationView(Guid Id, Guid BookingId, string Type, string Subject, string Body, DateTime CreatedAtUtc);

    private async Task<List<NotificationView>> WaitForNotificationsAsync(Guid userId, int expectedCount)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var list = await _client.GetFromJsonAsync<List<NotificationView>>($"/api/v1/notifications/user/{userId}");
            if (list is not null && list.Count >= expectedCount)
            {
                return list;
            }
            await Task.Delay(300);
        }
        return await _client.GetFromJsonAsync<List<NotificationView>>($"/api/v1/notifications/user/{userId}") ?? new();
    }

    [Fact]
    public async Task BookingConfirmed_creates_a_notification_in_history()
    {
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await _bus.Publish(new BookingConfirmed(bookingId, userId, Guid.NewGuid(), DateTime.UtcNow));

        List<NotificationView> history = await WaitForNotificationsAsync(userId, 1);

        history.Should().ContainSingle();
        history[0].Type.Should().Be("BookingConfirmed");
        history[0].BookingId.Should().Be(bookingId);
        history[0].Subject.Should().Contain("confirmed");
    }

    [Fact]
    public async Task BookingCancelled_creates_a_cancellation_notification()
    {
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await _bus.Publish(new BookingCancelled(bookingId, userId, "Payment failed", DateTime.UtcNow));

        List<NotificationView> history = await WaitForNotificationsAsync(userId, 1);

        history.Should().ContainSingle();
        history[0].Type.Should().Be("BookingCancelled");
        history[0].Body.Should().Contain("Payment failed");
    }

    [Fact]
    public async Task Duplicate_event_does_not_create_a_second_notification()
    {
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await _bus.Publish(new BookingConfirmed(bookingId, userId, Guid.NewGuid(), DateTime.UtcNow));
        await WaitForNotificationsAsync(userId, 1);
        await _bus.Publish(new BookingConfirmed(bookingId, userId, Guid.NewGuid(), DateTime.UtcNow));

        // Give the duplicate time to be (not) stored, then assert there is still exactly one.
        await Task.Delay(2000);
        var history = await _client.GetFromJsonAsync<List<NotificationView>>($"/api/v1/notifications/user/{userId}");
        history.Should().ContainSingle();
    }
}
