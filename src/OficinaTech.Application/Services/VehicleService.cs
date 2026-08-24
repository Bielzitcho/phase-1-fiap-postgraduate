using Mapster;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly IClientRepository _clientRepo;
    private readonly IUnitOfWork _uow;

    public VehicleService(IVehicleRepository repo, IClientRepository clientRepo, IUnitOfWork uow)
        => (_repo, _clientRepo, _uow) = (repo, clientRepo, uow);

    public async Task<Result<VehicleResponse>> CreateAsync(
        CreateVehicleRequest req, CancellationToken ct = default)
    {
        var client = await _clientRepo.GetByIdAsync(req.ClientId, ct);
        if (client is null)
            return Result<VehicleResponse>.Failure($"Client {req.ClientId} not found.");

        // LicensePlate VO validates format; DomainException propagates to global handler (D-06)
        var vehicle = new Vehicle(req.ClientId, new LicensePlate(req.LicensePlate), req.Make, req.Model, req.Year);
        await _repo.AddAsync(vehicle, ct);
        await _uow.CommitAsync(ct);
        return Result<VehicleResponse>.Success(vehicle.Adapt<VehicleResponse>());
    }

    public async Task<Result<VehicleResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await _repo.GetByIdAsync(id, ct);
        if (vehicle is null)
            return Result<VehicleResponse>.Failure($"Vehicle {id} not found.");
        return Result<VehicleResponse>.Success(vehicle.Adapt<VehicleResponse>());
    }

    public async Task<Result<PagedResult<VehicleResponse>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        var (items, total) = await _repo.GetAllAsync(page, pageSize, ct);
        var dtos = items.Select(v => v.Adapt<VehicleResponse>()).ToList();
        return Result<PagedResult<VehicleResponse>>.Success(
            new PagedResult<VehicleResponse>(dtos, total, page, pageSize));
    }

    public async Task<Result<VehicleResponse>> UpdateAsync(
        Guid id, UpdateVehicleRequest req, CancellationToken ct = default)
    {
        var vehicle = await _repo.GetByIdAsync(id, ct);
        if (vehicle is null)
            return Result<VehicleResponse>.Failure($"Vehicle {id} not found.");
        vehicle.UpdateDetails(req.Make, req.Model, req.Year);
        await _repo.UpdateAsync(vehicle, ct);
        await _uow.CommitAsync(ct);
        return Result<VehicleResponse>.Success(vehicle.Adapt<VehicleResponse>());
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await _repo.GetByIdAsync(id, ct);
        if (vehicle is null)
            return Result<bool>.Failure($"Vehicle {id} not found.");
        await _repo.DeleteAsync(id, ct);
        await _uow.CommitAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResult<VehicleResponse>>> GetByClientAsync(
        Guid clientId, int page, int pageSize, CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        var (items, total) = await _repo.GetByClientAsync(clientId, page, pageSize, ct);
        var dtos = items.Select(v => v.Adapt<VehicleResponse>()).ToList();
        return Result<PagedResult<VehicleResponse>>.Success(
            new PagedResult<VehicleResponse>(dtos, total, page, pageSize));
    }
}
