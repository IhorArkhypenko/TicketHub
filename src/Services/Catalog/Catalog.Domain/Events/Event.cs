using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Catalog.Domain.Events;

/// <summary>
/// Aggregate root for the afisha: an event (concert, movie, match) and its scheduled
/// sessions. DDD-lite — mostly read, with controlled creation of sessions.
/// </summary>
public sealed class Event : AggregateRoot<Guid>
{
    private readonly List<Session> _sessions = new();

    private Event(Guid id, string title, string description, string venue, EventCategory category) : base(id)
    {
        Title = title;
        Description = description;
        Venue = venue;
        Category = category;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private Event() { } // EF Core

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Venue { get; private set; } = null!;
    public EventCategory Category { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    public static Result<Event> Create(string title, string description, string venue, EventCategory category)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("Event.Title", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(venue))
        {
            return Error.Validation("Event.Venue", "Venue is required.");
        }

        return new Event(Guid.NewGuid(), title.Trim(), description?.Trim() ?? string.Empty, venue.Trim(), category);
    }

    /// <summary>Schedules a new session for this event and returns it.</summary>
    public Session AddSession(DateTime startsAtUtc)
    {
        var session = new Session(Guid.NewGuid(), Id, startsAtUtc);
        _sessions.Add(session);
        return session;
    }
}
