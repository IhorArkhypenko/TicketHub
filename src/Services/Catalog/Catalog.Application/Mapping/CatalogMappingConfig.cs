using Catalog.Application.Events;
using Catalog.Application.Seats;
using Catalog.Domain.Events;
using Catalog.Domain.Seats;
using Mapster;

namespace Catalog.Application.Mapping;

/// <summary>Mapster mappings from domain entities to DTOs, including value-object flattening.</summary>
public sealed class CatalogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Event, EventListItemDto>()
            .Map(dest => dest.Category, src => src.Category.ToString())
            .Map(dest => dest.SessionCount, src => src.Sessions.Count);

        config.NewConfig<Event, EventDetailsDto>()
            .Map(dest => dest.Category, src => src.Category.ToString())
            .Map(dest => dest.Sessions, src => src.Sessions);

        config.NewConfig<Session, SessionDto>();

        config.NewConfig<Seat, SeatDto>()
            .Map(dest => dest.Row, src => src.Number.Row)
            .Map(dest => dest.Number, src => src.Number.Number)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency)
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
