using BuildingBlocks.Infrastructure.Locking;
using Catalog.Application.Abstractions;
using Catalog.Domain.Seats;
using Contracts.Events.Catalog.V1;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Reserves a seat for a booking. A Redis distributed lock serializes the race for the same
/// seat; the Available→Held transition is guarded by the domain and by Postgres optimistic
/// concurrency, so two users can never both reserve one seat. Replies with SeatReserved or
/// SeatReservationRejected, correlated by BookingId, through the outbox.
/// </summary>
public sealed class ReserveSeatConsumer : IConsumer<ReserveSeat>
{
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(10);

    private readonly ISeatRepository _seats;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<ReserveSeatConsumer> _logger;

    public ReserveSeatConsumer(
        ISeatRepository seats,
        IUnitOfWork unitOfWork,
        IDistributedLock distributedLock,
        ILogger<ReserveSeatConsumer> logger)
    {
        _seats = seats;
        _unitOfWork = unitOfWork;
        _distributedLock = distributedLock;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveSeat> context)
    {
        Guid seatId = context.Message.SeatId;
        Guid bookingId = context.Message.BookingId;

        await using IAsyncDisposable? handle =
            await _distributedLock.AcquireAsync($"seat:{seatId}", LockTtl, LockWait, context.CancellationToken);

        if (handle is null)
        {
            await Reject(context, "Could not acquire the seat lock in time.");
            return;
        }

        Seat? seat = await _seats.GetByIdAsync(seatId, context.CancellationToken);
        if (seat is null)
        {
            await Reject(context, "Seat not found.");
            return;
        }

        var holdResult = seat.Hold();
        if (holdResult.IsFailure)
        {
            await Reject(context, holdResult.Error.Message);
            return;
        }

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        await context.Publish(new SeatReserved(bookingId, seatId, DateTime.UtcNow));
        _logger.LogInformation("Seat {SeatId} reserved for booking {BookingId}", seatId, bookingId);
    }

    private static Task Reject(ConsumeContext<ReserveSeat> context, string reason)
        => context.Publish(new SeatReservationRejected(context.Message.BookingId, context.Message.SeatId, reason, DateTime.UtcNow));
}

/// <summary>Confirms (sells) a previously held seat after payment.</summary>
public sealed class ConfirmSeatConsumer : IConsumer<ConfirmSeat>
{
    private readonly ISeatRepository _seats;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmSeatConsumer(ISeatRepository seats, IUnitOfWork unitOfWork)
    {
        _seats = seats;
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<ConfirmSeat> context)
    {
        Seat? seat = await _seats.GetByIdAsync(context.Message.SeatId, context.CancellationToken);
        if (seat is null)
        {
            return;
        }

        seat.MarkSold();
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}

/// <summary>Releases a held seat back to Available (compensation).</summary>
public sealed class ReleaseSeatConsumer : IConsumer<ReleaseSeat>
{
    private readonly ISeatRepository _seats;
    private readonly IUnitOfWork _unitOfWork;

    public ReleaseSeatConsumer(ISeatRepository seats, IUnitOfWork unitOfWork)
    {
        _seats = seats;
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<ReleaseSeat> context)
    {
        Seat? seat = await _seats.GetByIdAsync(context.Message.SeatId, context.CancellationToken);
        if (seat is null)
        {
            return;
        }

        // Releasing an already-available seat is a no-op (idempotent compensation).
        seat.Release();
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
