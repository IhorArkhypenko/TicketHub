using Payment.Domain;

namespace Payment.Application.Abstractions;

public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task AddAsync(PaymentRecord record, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken) where TEvent : class;
}

/// <summary>Result of a simulated provider charge.</summary>
public sealed record PaymentOutcome(bool Succeeded, string? FailureReason)
{
    public static PaymentOutcome Success() => new(true, null);
    public static PaymentOutcome Failure(string reason) => new(false, reason);
}

/// <summary>Simulates the external payment provider (deterministic for tests).</summary>
public interface IPaymentSimulator
{
    PaymentOutcome Charge(decimal amount, string currency);
}
