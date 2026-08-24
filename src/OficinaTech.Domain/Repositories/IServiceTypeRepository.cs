using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Domain.Repositories;

public interface IServiceTypeRepository
{
    Task<ServiceType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<ServiceType> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(ServiceType serviceType, CancellationToken ct = default);
    Task UpdateAsync(ServiceType serviceType, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
