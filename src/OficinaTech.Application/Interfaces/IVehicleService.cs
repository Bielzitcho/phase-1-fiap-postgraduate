using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IVehicleService
{
    Task<Result<VehicleResponse>> CreateAsync(CreateVehicleRequest req, CancellationToken ct = default);
    Task<Result<VehicleResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<VehicleResponse>>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<VehicleResponse>> UpdateAsync(Guid id, UpdateVehicleRequest req, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<VehicleResponse>>> GetByClientAsync(Guid clientId, int page, int pageSize, CancellationToken ct = default);
}
