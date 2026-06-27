using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Catalog.Domain.ValueObjects;

/// <summary>A seat's position within a session: a row label and a positive number.</summary>
public sealed class SeatNumber : ValueObject
{
    private SeatNumber(string row, int number)
    {
        Row = row;
        Number = number;
    }

    public string Row { get; }
    public int Number { get; }

    public static Result<SeatNumber> Create(string row, int number)
    {
        if (string.IsNullOrWhiteSpace(row))
        {
            return Error.Validation("SeatNumber.Row", "Row is required.");
        }

        if (number <= 0)
        {
            return Error.Validation("SeatNumber.Number", "Seat number must be positive.");
        }

        return new SeatNumber(row.Trim().ToUpperInvariant(), number);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Row;
        yield return Number;
    }

    public override string ToString() => $"{Row}-{Number}";
}
