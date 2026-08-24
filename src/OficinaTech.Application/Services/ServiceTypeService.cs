using Mapster;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Services;

public class ServiceTypeService : IServiceTypeService
{
    private readonly IServiceTypeRepository _repo;
    private readonly IUnitOfWork _uow;

    public ServiceTypeService(IServiceTypeRepository repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    public async Task<Result<ServiceTypeResponse>> CreateAsync(
        CreateServiceTypeRequest req, CancellationToken ct = default)
    {
        var serviceType = new ServiceType(req.Name, req.BasePrice, req.Description);
        await _repo.AddAsync(serviceType, ct);
        await _uow.CommitAsync(ct);
        return Result<ServiceTypeResponse>.Success(serviceType.Adapt<ServiceTypeResponse>());
    }

    public async Task<Result<ServiceTypeResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var serviceType = await _repo.GetByIdAsync(id, ct);
        if (serviceType is null)
            return Result<ServiceTypeResponse>.Failure($"ServiceType {id} not found.");
        return Result<ServiceTypeResponse>.Success(serviceType.Adapt<ServiceTypeResponse>());
    }

    public async Task<Result<PagedResult<ServiceTypeResponse>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        var (items, total) = await _repo.GetAllAsync(page, pageSize, ct);
        var dtos = items.Select(s => s.Adapt<ServiceTypeResponse>()).ToList();
        return Result<PagedResult<ServiceTypeResponse>>.Success(
            new PagedResult<ServiceTypeResponse>(dtos, total, page, pageSize));
    }

    public async Task<Result<ServiceTypeResponse>> UpdateAsync(
        Guid id, UpdateServiceTypeRequest req, CancellationToken ct = default)
    {
        var serviceType = await _repo.GetByIdAsync(id, ct);
        if (serviceType is null)
            return Result<ServiceTypeResponse>.Failure($"ServiceType {id} not found.");
        serviceType.UpdateName(req.Name);
        serviceType.UpdateBasePrice(req.BasePrice);
        serviceType.UpdateDescription(req.Description);
        await _repo.UpdateAsync(serviceType, ct);
        await _uow.CommitAsync(ct);
        return Result<ServiceTypeResponse>.Success(serviceType.Adapt<ServiceTypeResponse>());
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var serviceType = await _repo.GetByIdAsync(id, ct);
        if (serviceType is null)
            return Result<bool>.Failure($"ServiceType {id} not found.");
        await _repo.DeleteAsync(id, ct);
        await _uow.CommitAsync(ct);
        return Result<bool>.Success(true);
    }
}
