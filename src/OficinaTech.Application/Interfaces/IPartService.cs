using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IPartService
{
    Task<Result<PartResponse>> CreateAsync(CreatePartRequest req, CancellationToken ct = default);
    Task<Result<PartResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<PartResponse>>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PartResponse>> UpdateAsync(Guid id, UpdatePartRequest req, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
