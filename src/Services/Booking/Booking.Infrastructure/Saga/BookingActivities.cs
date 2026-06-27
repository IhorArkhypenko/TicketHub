using Booking.Application.Abstractions;
using Contracts.Events.Catalog.V1;
using Contracts.Events.Payment.V1;
using MassTransit;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.Infrastructure.Saga;

/// <summary>
/// Base for saga activities that apply a transition to the Booking aggregate (the rich domain
/// model is the source of truth). Loads the aggregate by the saga's correlation id, applies the
/// guarded transition, and saves — the UnitOfWork dispatches the resulting domain events as
/// integration events through the outbox.
/// </summary>
public abstract class BookingActivity<TMessage> : IStateMachineActivity<BookingState, TMessage>
    where TMessage : class
{
    protected readonly IBookingRepository Bookings;
    protected readonly IUnitOfWork UnitOfWork;

    protected BookingActivity(IBookingRepository bookings, IUnitOfWork unitOfWork)
    {
        Bookings = bookings;
        UnitOfWork = unitOfWork;
    }

    public void Probe(ProbeContext context) => context.CreateScope(GetType().Name);

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

    public async Task Execute(BehaviorContext<BookingState, TMessage> context, IBehavior<BookingState, TMessage> next)
    {
        BookingAggregate? booking = await Bookings.GetByIdAsync(context.Saga.CorrelationId, context.CancellationToken);
        if (booking is not null)
        {
            Apply(booking, context);
            await UnitOfWork.SaveChangesAsync(context.CancellationToken);
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<BookingState, TMessage, TException> context,
        IBehavior<BookingState, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    protected abstract void Apply(BookingAggregate booking, BehaviorContext<BookingState, TMessage> context);
}

public sealed class ApplySeatReservedActivity : BookingActivity<SeatReserved>
{
    public ApplySeatReservedActivity(IBookingRepository bookings, IUnitOfWork unitOfWork)
        : base(bookings, unitOfWork) { }

    protected override void Apply(BookingAggregate booking, BehaviorContext<BookingState, SeatReserved> context)
    {
        booking.MarkSeatReserved();
        booking.RequestPayment();
    }
}

public sealed class ConfirmBookingActivity : BookingActivity<PaymentCompleted>
{
    public ConfirmBookingActivity(IBookingRepository bookings, IUnitOfWork unitOfWork)
        : base(bookings, unitOfWork) { }

    protected override void Apply(BookingAggregate booking, BehaviorContext<BookingState, PaymentCompleted> context)
        => booking.Confirm();
}

public sealed class CancelOnPaymentFailedActivity : BookingActivity<PaymentFailed>
{
    public CancelOnPaymentFailedActivity(IBookingRepository bookings, IUnitOfWork unitOfWork)
        : base(bookings, unitOfWork) { }

    protected override void Apply(BookingAggregate booking, BehaviorContext<BookingState, PaymentFailed> context)
        => booking.Cancel(context.Message.Reason);
}

public sealed class CancelOnTimeoutActivity : BookingActivity<BookingHoldExpired>
{
    public CancelOnTimeoutActivity(IBookingRepository bookings, IUnitOfWork unitOfWork)
        : base(bookings, unitOfWork) { }

    protected override void Apply(BookingAggregate booking, BehaviorContext<BookingState, BookingHoldExpired> context)
        => booking.Cancel("Seat hold timed out before payment.");
}

public sealed class RejectBookingActivity : BookingActivity<SeatReservationRejected>
{
    public RejectBookingActivity(IBookingRepository bookings, IUnitOfWork unitOfWork)
        : base(bookings, unitOfWork) { }

    protected override void Apply(BookingAggregate booking, BehaviorContext<BookingState, SeatReservationRejected> context)
        => booking.Reject(context.Message.Reason);
}
