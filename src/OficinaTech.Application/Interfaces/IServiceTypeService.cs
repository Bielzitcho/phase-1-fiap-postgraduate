using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IServiceTypeService
{
    Task<Result<ServiceTypeResponse>> CreateAsync(CreateServiceTypeRequest req, CancellationToken ct = default);
    Task<Result<ServiceTypeResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<ServiceTypeResponse>>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<ServiceTypeResponse>> UpdateAsync(Guid id, UpdateServiceTypeRequest req, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
