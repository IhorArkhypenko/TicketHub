using BuildingBlocks.Domain.Primitives;

namespace Payment.Domain;

/// <summary>
/// A payment attempt for a booking. The <see cref="BookingId"/> is the idempotency key: there is
/// at most one payment record per booking, so a retried charge cannot debit twice.
/// </summary>
public sealed class PaymentRecord : AggregateRoot<Guid>
{
    private PaymentRecord(Guid id, Guid bookingId, decimal amount, string currency) : base(id)
    {
        BookingId = bookingId;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private PaymentRecord() { } // EF Core

    public Guid BookingId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    public static PaymentRecord Start(Guid bookingId, decimal amount, string currency)
        => new(Guid.NewGuid(), bookingId, amount, currency);

    public void MarkCompleted()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot complete a payment in status {Status}.");
        }

        Status = PaymentStatus.Completed;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot fail a payment in status {Status}.");
        }

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
