using System.Collections.Concurrent;
using Contracts.Events.Payment.V1;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;
using Xunit;

namespace Payment.IntegrationTests;

public sealed class PaymentTests : IClassFixture<PaymentApiFactory>, IAsyncLifetime
{
    private readonly PaymentApiFactory _factory;
    private IBusControl _bus = null!;

    private readonly ConcurrentDictionary<Guid, int> _completed = new();
    private readonly ConcurrentDictionary<Guid, PaymentFailed> _failed = new();

    public PaymentTests(PaymentApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _ = _factory.Services; // force the host (and its bus/migrations) to start

        _bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(_factory.RabbitMqHost, (ushort)_factory.RabbitMqPort, "/", h =>
            {
                h.Username(PaymentApiFactory.RabbitMqUser);
                h.Password(PaymentApiFactory.RabbitMqPassword);
            });

            cfg.ReceiveEndpoint("payment-test-events", e =>
            {
                e.Handler<PaymentCompleted>(ctx =>
                {
                    _completed.AddOrUpdate(ctx.Message.BookingId, 1, (_, count) => count + 1);
                    return Task.CompletedTask;
                });
                e.Handler<PaymentFailed>(ctx =>
                {
                    _failed[ctx.Message.BookingId] = ctx.Message;
                    return Task.CompletedTask;
                });
            });
        });

        await _bus.StartAsync();
    }

    public async Task DisposeAsync() => await _bus.StopAsync();

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(200);
        }
        return condition();
    }

    private async Task<int> PaymentRowCountAsync(Guid bookingId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        return await db.Payments.CountAsync(p => p.BookingId == bookingId);
    }

    [Fact]
    public async Task Valid_charge_completes_and_publishes_PaymentCompleted()
    {
        var bookingId = Guid.NewGuid();

        await _bus.Publish(new ProcessPayment(bookingId, 100m, "USD"));

        (await WaitUntilAsync(() => _completed.ContainsKey(bookingId), TimeSpan.FromSeconds(30)))
            .Should().BeTrue("the charge should complete and publish PaymentCompleted");
        (await PaymentRowCountAsync(bookingId)).Should().Be(1);
    }

    [Fact]
    public async Task Duplicate_command_with_same_key_does_not_charge_twice()
    {
        var bookingId = Guid.NewGuid();

        // Two independent messages (different messageIds) for the same booking key. The Inbox
        // dedups by messageId; the business-key idempotency must stop the second from charging.
        await _bus.Publish(new ProcessPayment(bookingId, 250m, "USD"));
        await WaitUntilAsync(() => _completed.GetValueOrDefault(bookingId) >= 1, TimeSpan.FromSeconds(30));

        await _bus.Publish(new ProcessPayment(bookingId, 250m, "USD"));
        // Wait for the second message to be handled (it re-publishes the existing outcome).
        await WaitUntilAsync(() => _completed.GetValueOrDefault(bookingId) >= 2, TimeSpan.FromSeconds(30));

        // Exactly one payment row — the second command did not debit again.
        (await PaymentRowCountAsync(bookingId)).Should().Be(1);
    }

    [Fact]
    public async Task Declined_amount_publishes_PaymentFailed()
    {
        var bookingId = Guid.NewGuid();

        await _bus.Publish(new ProcessPayment(bookingId, 13.13m, "USD"));

        (await WaitUntilAsync(() => _failed.ContainsKey(bookingId), TimeSpan.FromSeconds(30)))
            .Should().BeTrue("the declined amount should publish PaymentFailed");
        _failed[bookingId].Reason.Should().Contain("declined");
        (await PaymentRowCountAsync(bookingId)).Should().Be(1);
    }
}
