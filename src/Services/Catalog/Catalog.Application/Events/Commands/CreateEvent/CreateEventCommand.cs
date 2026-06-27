using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(
    string Title,
    string Description,
    string Venue,
    string Category) : ICommand<Guid>;
