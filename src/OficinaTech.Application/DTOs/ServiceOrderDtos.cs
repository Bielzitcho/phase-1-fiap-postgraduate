namespace OficinaTech.Application.DTOs;

// --- Request DTOs ---

public record CreateServiceOrderRequest(
    string TaxId,
    Guid VehicleId,
    IReadOnlyList<AddServiceRequest>? Services = null,
    IReadOnlyList<AddPartRequest>? Parts = null);

public record AddServiceRequest(Guid ServiceTypeId);

public record AddPartRequest(Guid PartId, int Quantity);

public record ApproveServiceOrderRequest(string TaxId);

// --- Response DTOs ---

public record ServiceOrderResponse(
    Guid Id,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? FinalizationDate,
    ClientSummary Client,
    VehicleSummary Vehicle,
    IReadOnlyList<OrderedServiceDto> OrderedServices,
    IReadOnlyList<OrderedPartDto> OrderedParts);

public record ServiceOrderSummaryResponse(
    Guid Id,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    Guid ClientId,
    Guid VehicleId);

public record ClientSummary(Guid Id, string Name, string TaxId);
public record VehicleSummary(Guid Id, string Plate, string Brand, string Model, int Year);
public record OrderedServiceDto(Guid ServiceTypeId, string Name, decimal UnitPrice);
public record OrderedPartDto(Guid PartId, string Name, decimal UnitPrice, int Quantity);
