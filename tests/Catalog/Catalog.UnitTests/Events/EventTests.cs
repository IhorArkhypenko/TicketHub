using Catalog.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests.Events;

public class EventTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Event.Create("Title", "Desc", "Venue", EventCategory.Movie);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Title");
        result.Value.Category.Should().Be(EventCategory.Movie);
    }

    [Theory]
    [InlineData("", "Venue", "Event.Title")]
    [InlineData("Title", "", "Event.Venue")]
    public void Create_WithMissingRequiredFields_Fails(string title, string venue, string expectedCode)
    {
        var result = Event.Create(title, "Desc", venue, EventCategory.Concert);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void AddSession_AppendsSessionForThisEvent()
    {
        var @event = Event.Create("T", "D", "V", EventCategory.Match).Value;

        var session = @event.AddSession(DateTime.UtcNow.AddDays(1));

        @event.Sessions.Should().ContainSingle();
        session.EventId.Should().Be(@event.Id);
    }
}
