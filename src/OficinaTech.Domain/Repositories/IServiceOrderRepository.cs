using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Enums;

namespace OficinaTech.Domain.Repositories;

public interface IServiceOrderRepository
{
    Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> GetByStatusAsync(ServiceOrderStatus status, CancellationToken ct = default);
    Task AddAsync(ServiceOrder serviceOrder, CancellationToken ct = default);
    Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
