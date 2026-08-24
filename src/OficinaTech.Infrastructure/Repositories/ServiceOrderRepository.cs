using Microsoft.EntityFrameworkCore;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Repositories;
using OficinaTech.Infrastructure.Data;

namespace OficinaTech.Infrastructure.Repositories;

public class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly OficinaTechDbContext _db;

    public ServiceOrderRepository(OficinaTechDbContext db) => _db = db;

    public async Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ServiceOrders.FindAsync([id], ct);

    public async Task<IReadOnlyList<ServiceOrder>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default)
        => await _db.ServiceOrders.Where(o => o.ClientId == clientId).ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceOrder>> GetByStatusAsync(ServiceOrderStatus status, CancellationToken ct = default)
        => await _db.ServiceOrders.Where(o => o.Status == status).ToListAsync(ct);

    public async Task AddAsync(ServiceOrder serviceOrder, CancellationToken ct = default)
        => await _db.ServiceOrders.AddAsync(serviceOrder, ct);

    public Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken ct = default)
    {
        _db.ServiceOrders.Update(serviceOrder);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var serviceOrder = await _db.ServiceOrders.FindAsync([id], ct);
        if (serviceOrder is not null)
            _db.ServiceOrders.Remove(serviceOrder);
    }
}
