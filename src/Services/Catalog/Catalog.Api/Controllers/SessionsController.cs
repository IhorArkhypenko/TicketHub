using Asp.Versioning;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Observability.Errors;
using Catalog.Application.Seats;
using Catalog.Application.Seats.Queries.GetSessionSeats;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISender _sender;

    public SessionsController(ISender sender) => _sender = sender;

    /// <summary>Lists the seats of a session with their current status.</summary>
    [HttpGet("{sessionId:guid}/seats")]
    [ProducesResponseType(typeof(IReadOnlyList<SeatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeatDto>>> GetSeats(Guid sessionId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<SeatDto>> result = await _sender.Send(new GetSessionSeatsQuery(sessionId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error);
    }
}
