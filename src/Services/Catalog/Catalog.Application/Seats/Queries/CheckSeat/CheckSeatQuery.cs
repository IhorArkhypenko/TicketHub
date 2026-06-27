using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Seats.Queries.CheckSeat;

/// <summary>Synchronous seat pre-check consumed by Booking over gRPC.</summary>
public sealed record CheckSeatQuery(Guid SeatId) : IQuery<SeatAvailabilityDto>;
