using Microsoft.Extensions.Configuration;
using Payment.Application.Abstractions;

namespace Payment.Infrastructure;

/// <summary>
/// Deterministic payment-provider simulation. Declines non-positive amounts and a configurable
/// "decline" amount (default 13.13), so tests can force a failure path on demand; otherwise it
/// approves. No randomness, so Saga compensation tests are reproducible.
/// </summary>
internal sealed class PaymentSimulator : IPaymentSimulator
{
    private readonly decimal _declineAmount;

    public PaymentSimulator(IConfiguration configuration)
        => _declineAmount = decimal.TryParse(configuration["Payment:DeclineAmount"], out decimal value) ? value : 13.13m;

    public PaymentOutcome Charge(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            return PaymentOutcome.Failure("Invalid amount.");
        }

        if (amount == _declineAmount)
        {
            return PaymentOutcome.Failure("Card declined by provider.");
        }

        return PaymentOutcome.Success();
    }
}
