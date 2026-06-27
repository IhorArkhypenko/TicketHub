using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain;
using Catalog.Domain.Events;
using Catalog.Domain.Seats;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Events.Commands.AddSession;

internal sealed class AddSessionCommandHandler : ICommandHandler<AddSessionCommand, Guid>
{
    private readonly IEventRepository _events;
    private readonly ISeatRepository _seats;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogCache _cache;

    public AddSessionCommandHandler(
        IEventRepository events,
        ISeatRepository seats,
        IUnitOfWork unitOfWork,
        ICatalogCache cache)
    {
        _events = events;
        _seats = seats;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result<Guid>> Handle(AddSessionCommand request, CancellationToken cancellationToken)
    {
        Event? @event = await _events.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            return Result.Failure<Guid>(CatalogErrors.EventNotFound(request.EventId));
        }

        if (HasDuplicateSeatNumbers(request.Seats))
        {
            return Result.Failure<Guid>(CatalogErrors.DuplicateSeatNumber);
        }

        Session session = @event.AddSession(request.StartsAtUtc);

        var newSeats = new List<Seat>(request.Seats.Count);
        foreach (SeatDefinition definition in request.Seats)
        {
            Result<SeatNumber> number = SeatNumber.Create(definition.Row, definition.Number);
            if (number.IsFailure)
            {
                return Result.Failure<Guid>(number.Error);
            }

            Result<Money> price = Money.Create(definition.Price, definition.Currency);
            if (price.IsFailure)
            {
                return Result.Failure<Guid>(price.Error);
            }

            newSeats.Add(Seat.Create(session.Id, number.Value, price.Value));
        }

        _seats.AddRange(newSeats);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CatalogCacheKeys.EventList, cancellationToken);
        await _cache.RemoveAsync(CatalogCacheKeys.EventDetails(@event.Id), cancellationToken);

        return session.Id;
    }

    private static bool HasDuplicateSeatNumbers(IReadOnlyList<SeatDefinition> seats) =>
        seats
            .Select(seat => (Row: seat.Row.Trim().ToUpperInvariant(), seat.Number))
            .Distinct()
            .Count() != seats.Count;
}
