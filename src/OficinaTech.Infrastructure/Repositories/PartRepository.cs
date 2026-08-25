using Microsoft.EntityFrameworkCore;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Infrastructure.Data;

namespace OficinaTech.Infrastructure.Repositories;

public class PartRepository : IPartRepository
{
    private readonly OficinaTechDbContext _db;

    public PartRepository(OficinaTechDbContext db) => _db = db;

    public async Task<Part?> GetByIdAsync(Guid id, CancellationToken ct = default)
        // Use FirstOrDefaultAsync (not FindAsync) to always fetch a fresh snapshot from the
        // database. FindAsync returns the already-tracked entity when it exists in the change
        // tracker, which produces a stale concurrency token baseline in ApproveAsync — the
        // optimistic concurrency check then operates against an outdated StockQuantity value,
        // potentially allowing overselling under concurrent approval requests.
        => await _db.Parts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Part> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Parts.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Part part, CancellationToken ct = default)
        => await _db.Parts.AddAsync(part, ct);

    public Task UpdateAsync(Part part, CancellationToken ct = default)
    {
        _db.Parts.Update(part);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var part = await _db.Parts.FindAsync([id], ct);
        if (part is not null)
            _db.Parts.Remove(part);
    }
}
