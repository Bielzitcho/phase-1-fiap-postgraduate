namespace OficinaTech.Application.DTOs;

public record CreatePartRequest(
    string Name,
    decimal UnitPrice,
    int StockQuantity,
    string? Description = null);

public record UpdatePartRequest(
    string Name,
    decimal UnitPrice,
    int StockQuantity,
    string? Description = null);

public record PartResponse(
    Guid Id,
    string Name,
    decimal UnitPrice,
    int StockQuantity,
    string? Description);
