using Microsoft.EntityFrameworkCore;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly OficinaTechDbContext _db;
    public EfUnitOfWork(OficinaTechDbContext db) => _db = db;

    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DomainException(
                "A record with the same unique identifier already exists.", ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";
}
