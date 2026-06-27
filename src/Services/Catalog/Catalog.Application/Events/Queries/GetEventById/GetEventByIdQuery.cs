using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid EventId) : IQuery<EventDetailsDto>;
