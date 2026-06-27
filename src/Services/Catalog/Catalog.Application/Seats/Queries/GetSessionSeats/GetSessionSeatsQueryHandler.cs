using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain;
using Catalog.Domain.Seats;
using Mapster;

namespace Catalog.Application.Seats.Queries.GetSessionSeats;

internal sealed class GetSessionSeatsQueryHandler : IQueryHandler<GetSessionSeatsQuery, IReadOnlyList<SeatDto>>
{
    private readonly ISeatRepository _seats;
    private readonly IEventRepository _events;

    public GetSessionSeatsQueryHandler(ISeatRepository seats, IEventRepository events)
    {
        _seats = seats;
        _events = events;
    }

    public async Task<Result<IReadOnlyList<SeatDto>>> Handle(
        GetSessionSeatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _events.SessionExistsAsync(request.SessionId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<SeatDto>>(CatalogErrors.SessionNotFound(request.SessionId));
        }

        IReadOnlyList<Seat> seats = await _seats.ListBySessionAsync(request.SessionId, cancellationToken);
        return Result.Success((IReadOnlyList<SeatDto>)seats.Adapt<List<SeatDto>>());
    }
}
