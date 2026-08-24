namespace OficinaTech.Application.DTOs;

public record CreateClientRequest(
    string Name,
    string TaxId,
    string Email,
    string Phone,
    string Address);

public record UpdateClientRequest(
    string Name,
    string Email,
    string Phone,
    string Address);

public record ClientResponse(
    Guid Id,
    string Name,
    string TaxId,
    string TaxIdType,
    string Email,
    string Phone,
    string Address);
