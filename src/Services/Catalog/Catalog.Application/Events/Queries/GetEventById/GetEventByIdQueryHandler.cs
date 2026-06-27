using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain;
using Catalog.Domain.Events;
using Mapster;

namespace Catalog.Application.Events.Queries.GetEventById;

internal sealed class GetEventByIdQueryHandler : IQueryHandler<GetEventByIdQuery, EventDetailsDto>
{
    private readonly IEventRepository _events;

    public GetEventByIdQueryHandler(IEventRepository events) => _events = events;

    public async Task<Result<EventDetailsDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        Event? @event = await _events.GetByIdAsync(request.EventId, cancellationToken);

        return @event is null
            ? Result.Failure<EventDetailsDto>(CatalogErrors.EventNotFound(request.EventId))
            : Result.Success(@event.Adapt<EventDetailsDto>());
    }
}
