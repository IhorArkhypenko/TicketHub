namespace Catalog.Domain.Seats;

/// <summary>Lifecycle of a seat. Available -> Held -> Sold, with reverse transitions on compensation.</summary>
public enum SeatStatus
{
    Available = 0,
    Held = 1,
    Sold = 2
}
