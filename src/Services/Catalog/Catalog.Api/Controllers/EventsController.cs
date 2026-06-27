using Asp.Versioning;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Observability.Errors;
using Catalog.Application.Events;
using Catalog.Application.Events.Commands.AddSession;
using Catalog.Application.Events.Commands.CreateEvent;
using Catalog.Application.Events.Queries.GetEventById;
using Catalog.Application.Events.Queries.GetEvents;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/events")]
public sealed class EventsController : ControllerBase
{
    private readonly ISender _sender;

    public EventsController(ISender sender) => _sender = sender;

    /// <summary>Lists the afisha (cached).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventListItemDto>>> GetEvents(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EventListItemDto>> result = await _sender.Send(new GetEventsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error);
    }

    /// <summary>Gets a single event with its sessions.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailsDto>> GetEventById(Guid id, CancellationToken cancellationToken)
    {
        Result<EventDetailsDto> result = await _sender.Send(new GetEventByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error);
    }

    /// <summary>Creates a new event.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateEvent(
        [FromBody] CreateEventCommand command,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetEventById), new { id = result.Value, version = "1.0" }, result.Value)
            : ApiResults.Problem(result.Error);
    }

    /// <summary>Adds a session (with its seats) to an event.</summary>
    [HttpPost("{id:guid}/sessions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddSession(
        Guid id,
        [FromBody] AddSessionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddSessionCommand(id, request.StartsAtUtc, request.Seats);
        Result<Guid> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/sessions/{result.Value}/seats", result.Value)
            : ApiResults.Problem(result.Error);
    }
}

/// <summary>Request body for adding a session; the event id comes from the route.</summary>
public sealed record AddSessionRequest(DateTime StartsAtUtc, IReadOnlyList<SeatDefinition> Seats);
