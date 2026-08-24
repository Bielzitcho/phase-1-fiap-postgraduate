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
        => await _db.Parts.FindAsync([id], ct);

    public async Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken ct = default)
        => await _db.Parts.ToListAsync(ct);

    public async Task AddAsync(Part part, CancellationToken ct = default)
    {
        await _db.Parts.AddAsync(part, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Part part, CancellationToken ct = default)
    {
        _db.Parts.Update(part);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var part = await _db.Parts.FindAsync([id], ct);
        if (part is not null)
        {
            _db.Parts.Remove(part);
            await _db.SaveChangesAsync(ct);
        }
    }
}
