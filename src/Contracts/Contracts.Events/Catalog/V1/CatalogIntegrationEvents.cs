namespace Contracts.Events.Catalog.V1;

// Versioned integration events published by Catalog. A breaking change is a NEW version
// (new type in a V2 namespace), never an edit to an existing contract.

/// <summary>A new session (with its seats) was added to the afisha.</summary>
public sealed record SessionScheduled(
    Guid EventId,
    Guid SessionId,
    DateTime StartsAtUtc,
    int SeatCount,
    DateTime OccurredOnUtc);

/// <summary>A seat moved to Held (reserved).</summary>
public sealed record SeatHeld(Guid SeatId, Guid SessionId, DateTime OccurredOnUtc);

/// <summary>A seat was released back to Available.</summary>
public sealed record SeatReleased(Guid SeatId, Guid SessionId, DateTime OccurredOnUtc);

/// <summary>A seat was sold (confirmed).</summary>
public sealed record SeatSold(Guid SeatId, Guid SessionId, DateTime OccurredOnUtc);
