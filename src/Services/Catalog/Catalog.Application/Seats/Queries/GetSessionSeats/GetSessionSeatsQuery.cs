using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Seats.Queries.GetSessionSeats;

public sealed record GetSessionSeatsQuery(Guid SessionId) : IQuery<IReadOnlyList<SeatDto>>;
