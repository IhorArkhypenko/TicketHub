using Catalog.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_Succeeds()
    {
        var result = Money.Create(99.50m, "usd");

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(99.50m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithNegativeAmount_Fails()
    {
        var result = Money.Create(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.Negative");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("")]
    public void Create_WithInvalidCurrency_Fails(string currency)
    {
        var result = Money.Create(10m, currency);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.Currency");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Money.Create(10m, "USD").Value;
        var b = Money.Create(10m, "USD").Value;

        a.Should().Be(b);
    }
}
