using MassTransit;

namespace Booking.Infrastructure.Saga;

/// <summary>
/// Persisted saga instance for the booking process. CorrelationId equals the BookingId, so the
/// saga and the Booking aggregate share the same identity.
/// </summary>
public sealed class BookingState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
    public string CurrentState { get; set; } = null!;

    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public Guid SeatId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string? CancellationReason { get; set; }

    // Token for the scheduled hold-timeout message, so it can be unscheduled on completion.
    public Guid? HoldTimeoutTokenId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Internal scheduled message that fires when the seat hold times out before payment.</summary>
public sealed record BookingHoldExpired
{
    public Guid BookingId { get; init; }
}
