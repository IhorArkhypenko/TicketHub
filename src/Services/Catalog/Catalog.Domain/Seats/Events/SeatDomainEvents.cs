using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Seats.Events;

public sealed record SeatHeldDomainEvent(Guid SeatId, Guid SessionId) : DomainEvent;

public sealed record SeatReleasedDomainEvent(Guid SeatId, Guid SessionId) : DomainEvent;

public sealed record SeatSoldDomainEvent(Guid SeatId, Guid SessionId) : DomainEvent;
