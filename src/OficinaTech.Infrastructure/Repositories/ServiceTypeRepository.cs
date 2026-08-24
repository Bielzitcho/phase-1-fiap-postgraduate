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

    public async Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default)
        => await _db.ServiceTypes.ToListAsync(ct);

    public async Task AddAsync(ServiceType serviceType, CancellationToken ct = default)
    {
        await _db.ServiceTypes.AddAsync(serviceType, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ServiceType serviceType, CancellationToken ct = default)
    {
        _db.ServiceTypes.Update(serviceType);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var serviceType = await _db.ServiceTypes.FindAsync([id], ct);
        if (serviceType is not null)
        {
            _db.ServiceTypes.Remove(serviceType);
            await _db.SaveChangesAsync(ct);
        }
    }
}
