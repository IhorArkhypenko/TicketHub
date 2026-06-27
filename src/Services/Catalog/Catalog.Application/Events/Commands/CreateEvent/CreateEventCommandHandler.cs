using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain.Events;

namespace Catalog.Application.Events.Commands.CreateEvent;

internal sealed class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogCache _cache;

    public CreateEventCommandHandler(IEventRepository events, IUnitOfWork unitOfWork, ICatalogCache cache)
    {
        _events = events;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var category = Enum.Parse<EventCategory>(request.Category, ignoreCase: true);

        Result<Event> eventResult = Event.Create(request.Title, request.Description, request.Venue, category);
        if (eventResult.IsFailure)
        {
            return Result.Failure<Guid>(eventResult.Error);
        }

        Event @event = eventResult.Value;
        await _events.AddAsync(@event, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Afisha changed — invalidate the cached list.
        await _cache.RemoveAsync(CatalogCacheKeys.EventList, cancellationToken);

        return @event.Id;
    }
}
