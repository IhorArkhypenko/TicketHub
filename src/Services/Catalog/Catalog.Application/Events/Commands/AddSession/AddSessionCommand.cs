using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Events.Commands.AddSession;

public sealed record SeatDefinition(string Row, int Number, decimal Price, string Currency);

public sealed record AddSessionCommand(
    Guid EventId,
    DateTime StartsAtUtc,
    IReadOnlyList<SeatDefinition> Seats) : ICommand<Guid>;
