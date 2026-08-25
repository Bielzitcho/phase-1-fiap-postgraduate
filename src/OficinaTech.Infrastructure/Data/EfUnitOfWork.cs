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
        catch (DbUpdateConcurrencyException ex)
        {
            // D-09: optimistic concurrency token mismatch (e.g., [ConcurrencyCheck] on Part.StockQuantity)
            // Mapped to 409 Conflict in the controller; ConcurrencyDomainException is a DomainException subtype
            // but the controller catches it before the global 400 handler processes it.
            throw new ConcurrencyDomainException(
                "The record was modified by another request. Please retry the operation.", ex);
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
