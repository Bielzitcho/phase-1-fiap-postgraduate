using Mapster;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Application.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _repo;
    private readonly IUnitOfWork _uow;

    public ClientService(IClientRepository repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    public async Task<Result<ClientResponse>> CreateAsync(
        CreateClientRequest req, CancellationToken ct = default)
    {
        // Domain constructor validates TaxId via TaxId VO — DomainException propagates (D-06)
        var client = new Client(req.Name, new TaxId(req.TaxId), req.Email, req.Phone, req.Address);
        await _repo.AddAsync(client, ct);
        await _uow.CommitAsync(ct);
        return Result<ClientResponse>.Success(client.Adapt<ClientResponse>());
    }

    public async Task<Result<ClientResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var client = await _repo.GetByIdAsync(id, ct);
        if (client is null)
            return Result<ClientResponse>.Failure($"Client {id} not found.");
        return Result<ClientResponse>.Success(client.Adapt<ClientResponse>());
    }

    public async Task<Result<PagedResult<ClientResponse>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        // D-12: cap pageSize to 100 silently (assumption A3)
        if (pageSize > 100) pageSize = 100;
        var (items, total) = await _repo.GetAllAsync(page, pageSize, ct);
        var dtos = items.Select(c => c.Adapt<ClientResponse>()).ToList();
        return Result<PagedResult<ClientResponse>>.Success(
            new PagedResult<ClientResponse>(dtos, total, page, pageSize));
    }

    public async Task<Result<ClientResponse>> UpdateAsync(
        Guid id, UpdateClientRequest req, CancellationToken ct = default)
    {
        var client = await _repo.GetByIdAsync(id, ct);
        if (client is null)
            return Result<ClientResponse>.Failure($"Client {id} not found.");
        client.UpdateName(req.Name);
        client.UpdateContactInfo(req.Email, req.Phone, req.Address);
        await _repo.UpdateAsync(client, ct);
        await _uow.CommitAsync(ct);
        return Result<ClientResponse>.Success(client.Adapt<ClientResponse>());
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var client = await _repo.GetByIdAsync(id, ct);
        if (client is null)
            return Result<bool>.Failure($"Client {id} not found.");
        await _repo.DeleteAsync(id, ct);
        await _uow.CommitAsync(ct);
        return Result<bool>.Success(true);
    }
}
