using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Domain.Repositories;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Part part, CancellationToken ct = default);
    Task UpdateAsync(Part part, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
