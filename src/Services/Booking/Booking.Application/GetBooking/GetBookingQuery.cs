using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Booking.Application.Abstractions;
using Booking.Domain;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.Application.GetBooking;

public sealed record BookingDto(Guid Id, string Status, Guid SeatId, decimal Amount, string Currency);

public sealed record GetBookingQuery(Guid BookingId) : IQuery<BookingDto>;

internal sealed class GetBookingQueryHandler : IQueryHandler<GetBookingQuery, BookingDto>
{
    private readonly IBookingRepository _bookings;

    public GetBookingQueryHandler(IBookingRepository bookings) => _bookings = bookings;

    public async Task<Result<BookingDto>> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        BookingAggregate? booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken);

        return booking is null
            ? Result.Failure<BookingDto>(BookingErrors.NotFound(request.BookingId))
            : Result.Success(new BookingDto(booking.Id, booking.Status.ToString(), booking.SeatId, booking.Amount, booking.Currency));
    }
}
