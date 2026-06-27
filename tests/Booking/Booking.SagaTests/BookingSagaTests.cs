using Booking.Application.Abstractions;
using Booking.Domain;
using Booking.Infrastructure.Saga;
using Contracts.Events.Booking.V1;
using Contracts.Events.Catalog.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.SagaTests;

public class BookingSagaTests
{
    private static ServiceProvider BuildProvider(int holdSeconds)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Booking:HoldTimeoutSeconds"] = holdSeconds.ToString() })
            .Build();

        return new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<InMemoryBookingRepository>()
            .AddScoped<IBookingRepository>(sp => sp.GetRequiredService<InMemoryBookingRepository>())
            .AddScoped<IUnitOfWork, TestUnitOfWork>()
            .AddScoped<ApplySeatReservedActivity>()
            .AddScoped<ConfirmBookingActivity>()
            .AddScoped<CancelOnPaymentFailedActivity>()
            .AddScoped<CancelOnTimeoutActivity>()
            .AddScoped<RejectBookingActivity>()
            .AddMassTransitTestHarness(x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddSagaStateMachine<BookingStateMachine, BookingState>().InMemoryRepository();
                x.AddConsumer<StubCatalogConsumer>();
                x.AddConsumer<StubPaymentConsumer>();
                x.UsingInMemory((ctx, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();
                    cfg.ConfigureEndpoints(ctx);
                });
            })
            .BuildServiceProvider(true);
    }

    private static async Task<Guid> StartBookingAsync(
        ITestHarness harness, InMemoryBookingRepository repo, decimal amount, Guid? seatId = null)
    {
        var booking = BookingAggregate.Submit(Guid.NewGuid(), Guid.NewGuid(), seatId ?? Guid.NewGuid(), amount, "USD");
        await repo.AddAsync(booking, default);
        await harness.Bus.Publish(new BookingSubmitted(
            booking.Id, booking.UserId, booking.SessionId, booking.SeatId, booking.Amount, booking.Currency));
        return booking.Id;
    }

    [Fact]
    public async Task Happy_path_reserves_pays_and_confirms()
    {
        await using ServiceProvider provider = BuildProvider(300);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var repo = provider.GetRequiredService<InMemoryBookingRepository>();
        Guid bookingId = await StartBookingAsync(harness, repo, amount: 100m);

        (await harness.Published.Any<BookingConfirmed>()).Should().BeTrue();
        (await harness.Published.Any<ConfirmSeat>()).Should().BeTrue();
        repo.Store[bookingId].Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Payment_failure_cancels_and_releases_the_seat()
    {
        await using ServiceProvider provider = BuildProvider(300);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var repo = provider.GetRequiredService<InMemoryBookingRepository>();
        Guid bookingId = await StartBookingAsync(harness, repo, amount: StubPaymentConsumer.DeclineAmount);

        (await harness.Published.Any<BookingCancelled>()).Should().BeTrue();
        (await harness.Published.Any<ReleaseSeat>()).Should().BeTrue();
        repo.Store[bookingId].Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Seat_rejection_rejects_the_booking()
    {
        await using ServiceProvider provider = BuildProvider(300);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var repo = provider.GetRequiredService<InMemoryBookingRepository>();
        Guid bookingId = await StartBookingAsync(harness, repo, amount: 100m, seatId: StubCatalogConsumer.TakenSeatId);

        (await harness.Published.Any<BookingRejected>()).Should().BeTrue();
        repo.Store[bookingId].Status.Should().Be(BookingStatus.Rejected);
    }

    [Fact]
    public async Task Hold_timeout_before_payment_cancels_and_releases()
    {
        await using ServiceProvider provider = BuildProvider(holdSeconds: 2);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var repo = provider.GetRequiredService<InMemoryBookingRepository>();
        // Payment never replies for this amount, so only the scheduled hold-timeout can resolve it.
        Guid bookingId = await StartBookingAsync(harness, repo, amount: StubPaymentConsumer.NoReplyAmount);

        DateTime deadline = DateTime.UtcNow.AddSeconds(25);
        while (DateTime.UtcNow < deadline && repo.Store[bookingId].Status != BookingStatus.Cancelled)
        {
            await Task.Delay(200);
        }

        repo.Store[bookingId].Status.Should().Be(BookingStatus.Cancelled);
        (await harness.Published.Any<ReleaseSeat>()).Should().BeTrue();
    }
}
