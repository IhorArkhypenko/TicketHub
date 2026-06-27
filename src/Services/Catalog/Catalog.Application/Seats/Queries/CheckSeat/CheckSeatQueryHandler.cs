using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain.Seats;

namespace Catalog.Application.Seats.Queries.CheckSeat;

internal sealed class CheckSeatQueryHandler : IQueryHandler<CheckSeatQuery, SeatAvailabilityDto>
{
    private readonly ISeatRepository _seats;

    public CheckSeatQueryHandler(ISeatRepository seats) => _seats = seats;

    public async Task<Result<SeatAvailabilityDto>> Handle(CheckSeatQuery request, CancellationToken cancellationToken)
    {
        Seat? seat = await _seats.GetByIdAsync(request.SeatId, cancellationToken);

        if (seat is null)
        {
            return Result.Success(new SeatAvailabilityDto(request.SeatId, Guid.Empty, Exists: false, Available: false, 0, string.Empty));
        }

        return Result.Success(new SeatAvailabilityDto(
            seat.Id,
            seat.SessionId,
            Exists: true,
            Available: seat.IsAvailable,
            seat.Price.Amount,
            seat.Price.Currency));
    }
}
