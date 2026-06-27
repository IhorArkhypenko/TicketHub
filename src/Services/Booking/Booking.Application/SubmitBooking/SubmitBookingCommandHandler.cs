using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Booking.Application.Abstractions;
using Booking.Domain;
using Contracts.Events.Booking.V1;
using BookingAggregate = Booking.Domain.Booking;

namespace Booking.Application.SubmitBooking;

/// <summary>
/// Accepts a booking request: synchronously pre-checks the seat with Catalog over gRPC, creates
/// the Pending Booking aggregate, and starts the Saga by publishing BookingSubmitted through the
/// outbox (transactional with the aggregate insert).
/// </summary>
internal sealed class SubmitBookingCommandHandler : ICommandHandler<SubmitBookingCommand, Guid>
{
    private readonly ISeatAvailabilityChecker _seatChecker;
    private readonly IBookingRepository _bookings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public SubmitBookingCommandHandler(
        ISeatAvailabilityChecker seatChecker,
        IBookingRepository bookings,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher)
    {
        _seatChecker = seatChecker;
        _bookings = bookings;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<Guid>> Handle(SubmitBookingCommand request, CancellationToken cancellationToken)
    {
        SeatAvailability seat = await _seatChecker.CheckAsync(request.SeatId, cancellationToken);

        if (!seat.Exists)
        {
            return Result.Failure<Guid>(BookingErrors.SeatNotFound);
        }

        if (!seat.Available)
        {
            return Result.Failure<Guid>(BookingErrors.SeatUnavailable);
        }

        BookingAggregate booking = BookingAggregate.Submit(
            request.UserId, seat.SessionId, request.SeatId, seat.Amount, seat.Currency);

        await _bookings.AddAsync(booking, cancellationToken);

        await _publisher.PublishAsync(
            new BookingSubmitted(booking.Id, booking.UserId, booking.SessionId, booking.SeatId, booking.Amount, booking.Currency),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.Id;
    }
}
