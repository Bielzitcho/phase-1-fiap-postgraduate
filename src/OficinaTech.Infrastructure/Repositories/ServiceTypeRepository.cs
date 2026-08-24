using Microsoft.EntityFrameworkCore;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Infrastructure.Data;

namespace OficinaTech.Infrastructure.Repositories;

public class ServiceTypeRepository : IServiceTypeRepository
{
    private readonly OficinaTechDbContext _db;

    public ServiceTypeRepository(OficinaTechDbContext db) => _db = db;

    public async Task<ServiceType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ServiceTypes.FindAsync([id], ct);

    public async Task<(IReadOnlyList<ServiceType> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ServiceTypes.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(ServiceType serviceType, CancellationToken ct = default)
        => await _db.ServiceTypes.AddAsync(serviceType, ct);

    public Task UpdateAsync(ServiceType serviceType, CancellationToken ct = default)
    {
        _db.ServiceTypes.Update(serviceType);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var serviceType = await _db.ServiceTypes.FindAsync([id], ct);
        if (serviceType is not null)
            _db.ServiceTypes.Remove(serviceType);
    }
}
