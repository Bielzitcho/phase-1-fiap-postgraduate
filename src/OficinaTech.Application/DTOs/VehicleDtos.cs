namespace OficinaTech.Application.DTOs;

public record CreateVehicleRequest(
    Guid ClientId,
    string LicensePlate,
    string Make,
    string Model,
    int Year);

public record UpdateVehicleRequest(
    string Make,
    string Model,
    int Year);

public record VehicleResponse(
    Guid Id,
    Guid ClientId,
    string LicensePlate,
    string Make,
    string Model,
    int Year);
