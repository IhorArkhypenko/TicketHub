namespace Contracts.Events.Payment.V1;

// Saga command consumed by Payment. BookingId is the idempotency key: a retried charge for
// the same booking must never debit twice.
public sealed record ProcessPayment(
    Guid BookingId,
    decimal Amount,
    string Currency);

// Events published by Payment via the outbox.
public sealed record PaymentCompleted(
    Guid BookingId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTime OccurredOnUtc);

public sealed record PaymentFailed(
    Guid BookingId,
    string Reason,
    DateTime OccurredOnUtc);
