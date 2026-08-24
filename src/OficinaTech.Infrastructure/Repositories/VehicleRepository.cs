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
    {
        await _db.Vehicles.AddAsync(vehicle, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _db.Vehicles.Update(vehicle);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await _db.Vehicles.FindAsync([id], ct);
        if (vehicle is not null)
        {
            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync(ct);
        }
    }
}
