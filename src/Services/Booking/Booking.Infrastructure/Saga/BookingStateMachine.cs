using Contracts.Events.Booking.V1;
using Contracts.Events.Catalog.V1;
using Contracts.Events.Payment.V1;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace Booking.Infrastructure.Saga;

/// <summary>
/// Orchestrates the booking process (chosen over choreography for a single, readable place to
/// reason about the flow and compensations):
///   Submitted → ReserveSeat → (SeatReserved → ProcessPayment → PaymentCompleted → ConfirmSeat → Confirmed)
/// with compensations on rejection, payment failure and hold timeout (ReleaseSeat + Cancel).
/// Commands are published (single consumer each); the hold timeout is a scheduled message.
/// </summary>
public sealed class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    public State AwaitingSeat { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;

    public Event<BookingSubmitted> BookingSubmitted { get; private set; } = null!;
    public Event<SeatReserved> SeatReserved { get; private set; } = null!;
    public Event<SeatReservationRejected> SeatReservationRejected { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailed { get; private set; } = null!;

    public Schedule<BookingState, BookingHoldExpired> HoldTimeout { get; private set; } = null!;

    public BookingStateMachine(IConfiguration configuration)
    {
        int holdSeconds = int.TryParse(configuration["Booking:HoldTimeoutSeconds"], out int seconds) ? seconds : 300;

        InstanceState(x => x.CurrentState);

        Event(() => BookingSubmitted, e => e.CorrelateById(m => m.Message.BookingId));
        Event(() => SeatReserved, e => e.CorrelateById(m => m.Message.BookingId));
        Event(() => SeatReservationRejected, e => e.CorrelateById(m => m.Message.BookingId));
        Event(() => PaymentCompleted, e => e.CorrelateById(m => m.Message.BookingId));
        Event(() => PaymentFailed, e => e.CorrelateById(m => m.Message.BookingId));

        Schedule(() => HoldTimeout, x => x.HoldTimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromSeconds(holdSeconds);
            s.Received = e => e.CorrelateById(m => m.Message.BookingId);
        });

        Initially(
            When(BookingSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.SessionId = ctx.Message.SessionId;
                    ctx.Saga.SeatId = ctx.Message.SeatId;
                    ctx.Saga.Amount = ctx.Message.Amount;
                    ctx.Saga.Currency = ctx.Message.Currency;
                    ctx.Saga.CreatedAtUtc = DateTime.UtcNow;
                })
                .Schedule(HoldTimeout, ctx => ctx.Init<BookingHoldExpired>(new { BookingId = ctx.Saga.CorrelationId }))
                .Publish(ctx => new ReserveSeat(ctx.Saga.CorrelationId, ctx.Saga.SeatId))
                .TransitionTo(AwaitingSeat));

        During(AwaitingSeat,
            When(SeatReserved)
                .Activity(x => x.OfType<ApplySeatReservedActivity>())
                .Publish(ctx => new ProcessPayment(ctx.Saga.CorrelationId, ctx.Saga.Amount, ctx.Saga.Currency))
                .TransitionTo(AwaitingPayment),
            When(SeatReservationRejected)
                .Activity(x => x.OfType<RejectBookingActivity>())
                .Unschedule(HoldTimeout)
                .Finalize());

        During(AwaitingPayment,
            When(PaymentCompleted)
                .Activity(x => x.OfType<ConfirmBookingActivity>())
                .Publish(ctx => new ConfirmSeat(ctx.Saga.CorrelationId, ctx.Saga.SeatId))
                .Unschedule(HoldTimeout)
                .Finalize(),
            When(PaymentFailed)
                .Activity(x => x.OfType<CancelOnPaymentFailedActivity>())
                .Publish(ctx => new ReleaseSeat(ctx.Saga.CorrelationId, ctx.Saga.SeatId))
                .Unschedule(HoldTimeout)
                .Finalize(),
            When(HoldTimeout.Received)
                .Activity(x => x.OfType<CancelOnTimeoutActivity>())
                .Publish(ctx => new ReleaseSeat(ctx.Saga.CorrelationId, ctx.Saga.SeatId))
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
