using BuildingBlocks.Application.Messaging;
using FluentValidation;

namespace Booking.Application.SubmitBooking;

public sealed record SubmitBookingCommand(Guid UserId, Guid SeatId) : ICommand<Guid>;

public sealed class SubmitBookingCommandValidator : AbstractValidator<SubmitBookingCommand>
{
    public SubmitBookingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SeatId).NotEmpty();
    }
}
