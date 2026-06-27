using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Events.Queries.GetEvents;

public sealed record GetEventsQuery : IQuery<IReadOnlyList<EventListItemDto>>;
