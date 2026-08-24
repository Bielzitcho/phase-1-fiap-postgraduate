namespace OficinaTech.Application.DTOs;

public record CreateServiceTypeRequest(
    string Name,
    decimal BasePrice,
    string? Description = null);

public record UpdateServiceTypeRequest(
    string Name,
    decimal BasePrice,
    string? Description = null);

public record ServiceTypeResponse(
    Guid Id,
    string Name,
    decimal BasePrice,
    string? Description);
