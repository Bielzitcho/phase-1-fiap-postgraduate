using Microsoft.EntityFrameworkCore;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.ValueObjects;
using OficinaTech.Infrastructure.Data;

namespace OficinaTech.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly OficinaTechDbContext _db;

    public VehicleRepository(OficinaTechDbContext db) => _db = db;

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Vehicles.FindAsync([id], ct);

    public async Task<Vehicle?> GetByLicensePlateAsync(string plate, CancellationToken ct = default)
        => await _db.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == new LicensePlate(plate), ct);

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct = default)
        => await _db.Vehicles.AddAsync(vehicle, ct);

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _db.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await _db.Vehicles.FindAsync([id], ct);
        if (vehicle is not null)
            _db.Vehicles.Remove(vehicle);
    }

    public async Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Vehicles.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.LicensePlate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> GetByClientAsync(
        Guid clientId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Vehicles.Where(v => v.ClientId == clientId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.LicensePlate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}
