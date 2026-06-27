using Booking.Domain;
using Booking.Domain.Events;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.UnitTests;

public class BookingAggregateTests
{
    private static BookingAggregate NewBooking()
        => BookingAggregate.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");

    private static BookingAggregate AwaitingPayment()
    {
        var booking = NewBooking();
        booking.MarkSeatReserved();
        booking.RequestPayment();
        return booking;
    }

    [Fact]
    public void Submit_startsPending_andRaisesSubmittedEvent()
    {
        var booking = NewBooking();

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.DomainEvents.Should().ContainSingle(e => e is BookingSubmittedDomainEvent);
    }

    [Fact]
    public void Happy_path_transitions_to_confirmed()
    {
        var booking = NewBooking();

        booking.MarkSeatReserved().IsSuccess.Should().BeTrue();
        booking.RequestPayment().IsSuccess.Should().BeTrue();
        booking.Confirm().IsSuccess.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.DomainEvents.Should().Contain(e => e is BookingConfirmedDomainEvent);
    }

    [Fact]
    public void Cannot_confirm_an_unpaid_booking()
    {
        var booking = NewBooking();
        booking.MarkSeatReserved(); // still SeatReserved, payment not requested/done

        var result = booking.Confirm();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.CannotConfirmUnpaid);
        booking.Status.Should().Be(BookingStatus.SeatReserved);
    }

    [Fact]
    public void Cannot_confirm_directly_from_pending()
    {
        var booking = NewBooking();

        booking.Confirm().IsFailure.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public void Cannot_cancel_a_confirmed_booking()
    {
        var booking = AwaitingPayment();
        booking.Confirm();

        var result = booking.Cancel("changed mind");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.CannotCancelConfirmed);
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Cancel_from_awaiting_payment_succeeds()
    {
        var booking = AwaitingPayment();

        booking.Cancel("payment failed").IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.DomainEvents.Should().Contain(e => e is BookingCancelledDomainEvent);
    }

    [Fact]
    public void Reject_from_pending_succeeds_but_not_after_confirmed()
    {
        var booking = NewBooking();
        booking.Reject("seat taken").IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Rejected);

        // a terminal booking cannot be rejected again
        booking.Reject("again").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RequestPayment_requires_seat_reserved_first()
    {
        var booking = NewBooking();

        booking.RequestPayment().IsFailure.Should().BeTrue();
    }
}
