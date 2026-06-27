using FluentAssertions;
using Payment.Domain;
using Xunit;

namespace Payment.UnitTests;

public class PaymentRecordTests
{
    [Fact]
    public void Start_createsPendingPayment()
    {
        var payment = PaymentRecord.Start(Guid.NewGuid(), 100m, "USD");

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkCompleted_setsCompletedAndTimestamp()
    {
        var payment = PaymentRecord.Start(Guid.NewGuid(), 100m, "USD");

        payment.MarkCompleted();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_setsFailedWithReason()
    {
        var payment = PaymentRecord.Start(Guid.NewGuid(), 100m, "USD");

        payment.MarkFailed("declined");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("declined");
    }

    [Fact]
    public void Cannot_transition_an_already_processed_payment()
    {
        var payment = PaymentRecord.Start(Guid.NewGuid(), 100m, "USD");
        payment.MarkCompleted();

        var act = () => payment.MarkFailed("x");

        act.Should().Throw<InvalidOperationException>();
    }
}
