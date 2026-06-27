namespace Catalog.Application.Seats;

public sealed record SeatDto(
    Guid Id,
    Guid SessionId,
    string Row,
    int Number,
    decimal Price,
    string Currency,
    string Status);

/// <summary>Returned by the gRPC seat pre-check used by Booking.</summary>
public sealed record SeatAvailabilityDto(
    Guid SeatId,
    Guid SessionId,
    bool Exists,
    bool Available,
    decimal Price,
    string Currency);
