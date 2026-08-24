using Mapster;
using NSubstitute;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Application.Mapping;
using OficinaTech.Application.Services;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.Services;

public class ClientServiceTests
{
    // Valid CPF used throughout: 529.982.247-25 (matches existing TaxIdTests)
    private const string ValidCpf = "529.982.247-25";
    private const string ValidEmail = "john@example.com";
    private const string ValidPhone = "11999999999";
    private const string ValidAddress = "Rua A, 1";
    private const string ValidName = "John Doe";

    private readonly IClientRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        // Call MappingConfig.Register() once per test run so Adapt<ClientResponse>() works
        MappingConfig.Register();

        _repo = Substitute.For<IClientRepository>();
        _uow = Substitute.For<IUnitOfWork>();
        _service = new ClientService(_repo, _uow);
    }

    private static Client MakeClient(string name = ValidName)
        => new Client(name, new TaxId(ValidCpf), ValidEmail, ValidPhone, ValidAddress);

    // -----------------------------------------------------------------------
    // CreateAsync — happy path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccessWithClientResponse()
    {
        var req = new CreateClientRequest(ValidName, ValidCpf, ValidEmail, ValidPhone, ValidAddress);

        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ValidCpf.Replace(".", "").Replace("-", ""), result.Value!.TaxId);
        Assert.Equal(ValidAddress, result.Value.Address);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CallsCommitAsyncOnce()
    {
        var req = new CreateClientRequest(ValidName, ValidCpf, ValidEmail, ValidPhone, ValidAddress);

        await _service.CreateAsync(req);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // CreateAsync — invalid CPF propagates DomainException (D-06)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithInvalidCpf_ThrowsDomainException()
    {
        var req = new CreateClientRequest(ValidName, "123", ValidEmail, ValidPhone, ValidAddress);

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(req));
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync — not found returns failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailureWithNotFoundMessage()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Client?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Client {id} not found.", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var client = MakeClient();
        _repo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        var result = await _service.GetByIdAsync(client.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(client.Id, result.Value!.Id);
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — pageSize clamped to 100
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenPageSizeOver100_ClampsTo100()
    {
        var clients = new List<Client>().AsReadOnly();
        _repo.GetAllAsync(1, 100, Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(((IReadOnlyList<Client>)clients, 0)));

        var result = await _service.GetAllAsync(1, 500);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.PageSize);
        // Repo was called with clamped value 100 (not 500)
        await _repo.Received(1).GetAllAsync(1, 100, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Client?)null);
        var req = new UpdateClientRequest("NewName", ValidEmail, ValidPhone, ValidAddress);

        var result = await _service.UpdateAsync(id, req);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Client {id} not found.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndCallsCommitOnce()
    {
        var client = MakeClient();
        _repo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var req = new UpdateClientRequest("Updated Name", "new@email.com", "11888888888", "Rua B, 2");

        var result = await _service.UpdateAsync(client.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value!.Name);
        Assert.Equal("new@email.com", result.Value.Email);
        Assert.Equal("Rua B, 2", result.Value.Address);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Client?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Client {id} not found.", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ReturnsSuccessAndCallsCommitOnce()
    {
        var client = MakeClient();
        _repo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        var result = await _service.DeleteAsync(client.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
