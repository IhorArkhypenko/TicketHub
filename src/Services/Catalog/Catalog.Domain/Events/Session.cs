using BuildingBlocks.Domain.Primitives;

namespace Catalog.Domain.Events;

/// <summary>
/// A scheduled showing of an event in time. Child entity of the <see cref="Event"/>
/// aggregate; seats reference it by id.
/// </summary>
public sealed class Session : Entity<Guid>
{
    internal Session(Guid id, Guid eventId, DateTime startsAtUtc) : base(id)
    {
        EventId = eventId;
        StartsAtUtc = startsAtUtc;
    }

    private Session() { } // EF Core

    public Guid EventId { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
}
