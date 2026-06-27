namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Marker for an event that happened inside the domain. Raised by aggregates and
/// dispatched after the aggregate is persisted.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
