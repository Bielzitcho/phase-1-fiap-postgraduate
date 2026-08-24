using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IClientService
{
    Task<Result<ClientResponse>> CreateAsync(CreateClientRequest req, CancellationToken ct = default);
    Task<Result<ClientResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<ClientResponse>>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<ClientResponse>> UpdateAsync(Guid id, UpdateClientRequest req, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
