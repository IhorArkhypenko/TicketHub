using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Seats;
using Catalog.Application.Seats.Queries.CheckSeat;
using Contracts.Protos.Catalog;
using Grpc.Core;

namespace Catalog.Api.Grpc;

/// <summary>
/// gRPC endpoint for the synchronous seat pre-check. Chosen over REST for the internal
/// Booking -> Catalog call: low latency and a strict contract.
/// </summary>
public sealed class CatalogSeatCheckService : CatalogSeatCheck.CatalogSeatCheckBase
{
    private readonly ISender _sender;

    public CatalogSeatCheckService(ISender sender) => _sender = sender;

    public override async Task<CheckSeatReply> CheckSeat(CheckSeatRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.SeatId, out Guid seatId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "seat_id must be a GUID."));
        }

        Result<SeatAvailabilityDto> result = await _sender.Send(new CheckSeatQuery(seatId), context.CancellationToken);

        if (result.IsFailure)
        {
            throw new RpcException(new Status(StatusCode.Internal, result.Error.Message));
        }

        SeatAvailabilityDto seat = result.Value;
        return new CheckSeatReply
        {
            Exists = seat.Exists,
            Available = seat.Available,
            SessionId = seat.Exists ? seat.SessionId.ToString() : string.Empty,
            Amount = (double)seat.Price,
            Currency = seat.Currency
        };
    }
}
