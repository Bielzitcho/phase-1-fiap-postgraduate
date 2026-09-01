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

    public async Task<ServiceOrder?> GetByIdWithIncludesAsync(Guid id, CancellationToken ct = default)
        => await _db.ServiceOrders
            .Include("_orderedServices")
            .Include("_orderedParts")
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(IReadOnlyList<ServiceOrder> Items, int TotalCount)> GetAllAsync(
        ServiceOrderStatus? status, Guid? clientId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ServiceOrders.AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (clientId.HasValue) query = query.Where(o => o.ClientId == clientId.Value);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            // Load the child collections so ServiceOrder.TotalAmount (computed from them) is not 0.
            // AsSplitQuery avoids the cartesian blow-up of joining two collections in one query.
            .Include("_orderedServices")
            .Include("_orderedParts")
            .AsSplitQuery()
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<ServiceOrder>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default)
        => await _db.ServiceOrders
            .Where(o => o.ClientId == clientId)
            // Same rationale as GetAllAsync: TotalAmount is computed from the child collections.
            .Include("_orderedServices")
            .Include("_orderedParts")
            .AsSplitQuery()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceOrder>> GetByStatusAsync(ServiceOrderStatus status, CancellationToken ct = default)
        => await _db.ServiceOrders
            .Where(o => o.Status == status)
            .Include("_orderedServices")
            .Include("_orderedParts")
            .AsSplitQuery()
            .ToListAsync(ct);

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
