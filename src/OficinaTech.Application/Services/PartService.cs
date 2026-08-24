using Mapster;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Services;

public class PartService : IPartService
{
    private readonly IPartRepository _repo;
    private readonly IUnitOfWork _uow;

    public PartService(IPartRepository repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    public async Task<Result<PartResponse>> CreateAsync(
        CreatePartRequest req, CancellationToken ct = default)
    {
        var part = new Part(req.Name, req.UnitPrice, req.StockQuantity, req.Description);
        await _repo.AddAsync(part, ct);
        await _uow.CommitAsync(ct);
        return Result<PartResponse>.Success(part.Adapt<PartResponse>());
    }

    public async Task<Result<PartResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var part = await _repo.GetByIdAsync(id, ct);
        if (part is null)
            return Result<PartResponse>.Failure($"Part {id} not found.");
        return Result<PartResponse>.Success(part.Adapt<PartResponse>());
    }

    public async Task<Result<PagedResult<PartResponse>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        var (items, total) = await _repo.GetAllAsync(page, pageSize, ct);
        var dtos = items.Select(p => p.Adapt<PartResponse>()).ToList();
        return Result<PagedResult<PartResponse>>.Success(
            new PagedResult<PartResponse>(dtos, total, page, pageSize));
    }

    public async Task<Result<PartResponse>> UpdateAsync(
        Guid id, UpdatePartRequest req, CancellationToken ct = default)
    {
        var part = await _repo.GetByIdAsync(id, ct);
        if (part is null)
            return Result<PartResponse>.Failure($"Part {id} not found.");
        part.UpdateDetails(req.Name, req.UnitPrice, req.StockQuantity, req.Description);
        await _repo.UpdateAsync(part, ct);
        await _uow.CommitAsync(ct);
        return Result<PartResponse>.Success(part.Adapt<PartResponse>());
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var part = await _repo.GetByIdAsync(id, ct);
        if (part is null)
            return Result<bool>.Failure($"Part {id} not found.");
        await _repo.DeleteAsync(id, ct);
        await _uow.CommitAsync(ct);
        return Result<bool>.Success(true);
    }
}
