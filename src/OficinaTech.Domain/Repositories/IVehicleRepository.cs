using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Domain.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Vehicle?> GetByLicensePlateAsync(string plate, CancellationToken ct = default);
    Task AddAsync(Vehicle vehicle, CancellationToken ct = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
