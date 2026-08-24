using Mapster;
using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Application.Mapping;

public static class MappingConfig
{
    public static void Register()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfig).Assembly);

        // Explicit rules: Value Object (TaxId) to string properties
        TypeAdapterConfig<Client, ClientResponse>.NewConfig()
            .Map(dest => dest.TaxId, src => src.TaxId.Value)
            .Map(dest => dest.TaxIdType, src => src.TaxId.Type.ToString());

        // Explicit rule: LicensePlate VO to string (Pitfall 4 — auto-map fails on nested .Value)
        TypeAdapterConfig<Vehicle, VehicleResponse>.NewConfig()
            .Map(dest => dest.LicensePlate, src => src.LicensePlate.Value);
    }
}
