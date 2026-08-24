using OficinaTech.Application.Interfaces;

namespace OficinaTech.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly OficinaTechDbContext _db;
    public EfUnitOfWork(OficinaTechDbContext db) => _db = db;
    public Task CommitAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
