namespace Catalog.Application.Events;

public sealed record EventListItemDto(
    Guid Id,
    string Title,
    string Venue,
    string Category,
    int SessionCount);

public sealed record SessionDto(
    Guid Id,
    DateTime StartsAtUtc);

public sealed record EventDetailsDto(
    Guid Id,
    string Title,
    string Description,
    string Venue,
    string Category,
    DateTime CreatedAtUtc,
    IReadOnlyList<SessionDto> Sessions);
