using BuildingBlocks.Domain.Results;

namespace Catalog.Domain;

/// <summary>Expected business errors raised by the Catalog domain and application layers.</summary>
public static class CatalogErrors
{
    public static Error EventNotFound(Guid id) =>
        Error.NotFound("Catalog.EventNotFound", $"Event '{id}' was not found.");

    public static Error SessionNotFound(Guid id) =>
        Error.NotFound("Catalog.SessionNotFound", $"Session '{id}' was not found.");

    public static Error SeatNotFound(Guid id) =>
        Error.NotFound("Catalog.SeatNotFound", $"Seat '{id}' was not found.");

    public static readonly Error SeatNotAvailable =
        Error.Conflict("Catalog.SeatNotAvailable", "The seat is not available to hold.");

    public static readonly Error SeatNotHeld =
        Error.Conflict("Catalog.SeatNotHeld", "The seat is not currently held.");

    public static readonly Error SessionMustHaveSeats =
        Error.Validation("Catalog.SessionMustHaveSeats", "A session must define at least one seat.");

    public static readonly Error DuplicateSeatNumber =
        Error.Validation("Catalog.DuplicateSeatNumber", "Seat numbers within a session must be unique.");
}
