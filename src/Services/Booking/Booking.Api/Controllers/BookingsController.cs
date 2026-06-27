using System.Security.Claims;
using Asp.Versioning;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Observability.Errors;
using BuildingBlocks.Observability.Security;
using Booking.Application.GetBooking;
using Booking.Application.SubmitBooking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly ISender _sender;

    public BookingsController(ISender sender) => _sender = sender;

    /// <summary>Submits a booking for a seat, starting the Saga. Requires a valid JWT (booking.api scope).</summary>
    [HttpPost]
    [Authorize(Policy = JwtAuthenticationExtensions.ScopePolicy)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Submit([FromBody] SubmitBookingRequest request, CancellationToken cancellationToken)
    {
        Guid userId = ResolveUserId(request.UserId);

        Result<Guid> result = await _sender.Send(new SubmitBookingCommand(userId, request.SeatId), cancellationToken);
        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetStatus), new { id = result.Value, version = "1.0" }, result.Value)
            : ApiResults.Problem(result.Error);
    }

    /// <summary>Returns the current booking status (poll while the Saga runs).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = JwtAuthenticationExtensions.ScopePolicy)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        Result<BookingDto> result = await _sender.Send(new GetBookingQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error);
    }

    private Guid ResolveUserId(Guid? fromBody)
    {
        string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out Guid userId))
        {
            return userId;
        }

        // Machine tokens (client-credentials) carry no user; fall back to the supplied id.
        return fromBody ?? Guid.NewGuid();
    }
}

public sealed record SubmitBookingRequest(Guid SeatId, Guid? UserId);
