using Microsoft.EntityFrameworkCore;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.ValueObjects;
using OficinaTech.Infrastructure.Data;

namespace OficinaTech.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly OficinaTechDbContext _db;

    public ClientRepository(OficinaTechDbContext db) => _db = db;

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Clients.FindAsync([id], ct);

    public async Task<Client?> GetByTaxIdAsync(string taxIdValue, CancellationToken ct = default)
        => await _db.Clients.FirstOrDefaultAsync(c => c.TaxId == new TaxId(taxIdValue), ct);

    public async Task AddAsync(Client client, CancellationToken ct = default)
        => await _db.Clients.AddAsync(client, ct);

    public Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Update(client);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var client = await _db.Clients.FindAsync([id], ct);
        if (client is not null)
            _db.Clients.Remove(client);
    }

    public async Task<(IReadOnlyList<Client> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Clients.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}
