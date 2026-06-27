using BuildingBlocks.Domain.Results;

namespace Booking.Domain;

public static class BookingErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Booking.NotFound", $"Booking '{id}' was not found.");

    public static readonly Error CannotConfirmUnpaid =
        Error.Conflict("Booking.CannotConfirmUnpaid", "A booking can only be confirmed after payment (from AwaitingPayment).");

    public static readonly Error CannotCancelConfirmed =
        Error.Conflict("Booking.CannotCancelConfirmed", "A confirmed booking cannot be cancelled.");

    public static readonly Error InvalidSeatReservation =
        Error.Conflict("Booking.InvalidSeatReservation", "Seat can only be reserved while the booking is Pending.");

    public static readonly Error InvalidPaymentRequest =
        Error.Conflict("Booking.InvalidPaymentRequest", "Payment can only be requested after the seat is reserved.");

    public static readonly Error AlreadyTerminal =
        Error.Conflict("Booking.AlreadyTerminal", "The booking has already reached a terminal state.");
}
