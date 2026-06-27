using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Catalog.Domain.ValueObjects;

/// <summary>Monetary amount with currency. Immutable; non-negative.</summary>
public sealed class Money : ValueObject
{
    public const string DefaultCurrency = "USD";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            return Error.Validation("Money.Negative", "Amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Error.Validation("Money.Currency", "Currency must be a 3-letter ISO code.");
        }

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
