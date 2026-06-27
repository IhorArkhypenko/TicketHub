using Catalog.Domain.Seats;
using Catalog.Domain.Seats.Events;
using Catalog.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests.Seats;

public class SeatTests
{
    private static Seat NewSeat()
    {
        var number = SeatNumber.Create("A", 1).Value;
        var price = Money.Create(50m, "USD").Value;
        return Seat.Create(Guid.NewGuid(), number, price);
    }

    [Fact]
    public void NewSeat_IsAvailable()
    {
        var seat = NewSeat();
        seat.Status.Should().Be(SeatStatus.Available);
        seat.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Hold_FromAvailable_MovesToHeld_AndRaisesEvent()
    {
        var seat = NewSeat();

        var result = seat.Hold();

        result.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Held);
        seat.DomainEvents.Should().ContainSingle(e => e is SeatHeldDomainEvent);
    }

    [Fact]
    public void Hold_WhenNotAvailable_Fails()
    {
        var seat = NewSeat();
        seat.Hold();

        var result = seat.Hold();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Catalog.Domain.CatalogErrors.SeatNotAvailable);
    }

    [Fact]
    public void MarkSold_RequiresHeld()
    {
        var seat = NewSeat();

        var sellWhileAvailable = seat.MarkSold();
        sellWhileAvailable.IsFailure.Should().BeTrue();

        seat.Hold();
        var sellWhileHeld = seat.MarkSold();
        sellWhileHeld.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Sold);
    }

    [Fact]
    public void Release_FromHeld_MovesBackToAvailable()
    {
        var seat = NewSeat();
        seat.Hold();

        var result = seat.Release();

        result.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Available);
        seat.DomainEvents.Should().Contain(e => e is SeatReleasedDomainEvent);
    }

    [Fact]
    public void Release_WhenNotHeld_Fails()
    {
        var seat = NewSeat();

        var result = seat.Release();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Catalog.Domain.CatalogErrors.SeatNotHeld);
    }
}
