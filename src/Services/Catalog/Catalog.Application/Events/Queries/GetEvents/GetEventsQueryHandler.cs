using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain.Events;
using Mapster;

namespace Catalog.Application.Events.Queries.GetEvents;

internal sealed class GetEventsQueryHandler
    : IQueryHandler<GetEventsQuery, IReadOnlyList<EventListItemDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IEventRepository _events;
    private readonly ICatalogCache _cache;

    public GetEventsQueryHandler(IEventRepository events, ICatalogCache cache)
    {
        _events = events;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<EventListItemDto>>> Handle(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Cache-aside: the afisha is read-heavy and changes rarely.
        IReadOnlyList<EventListItemDto>? cached =
            await _cache.GetAsync<IReadOnlyList<EventListItemDto>>(CatalogCacheKeys.EventList, cancellationToken);

        if (cached is not null)
        {
            return Result.Success(cached);
        }

        IReadOnlyList<Event> events = await _events.ListAsync(cancellationToken);
        IReadOnlyList<EventListItemDto> dtos = events.Adapt<List<EventListItemDto>>();

        await _cache.SetAsync(CatalogCacheKeys.EventList, dtos, CacheTtl, cancellationToken);

        return Result.Success(dtos);
    }
}
