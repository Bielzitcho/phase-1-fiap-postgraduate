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

public class VehicleServiceTests
{
    private const string ValidCpf = "529.982.247-25";
    private const string ValidEmail = "john@example.com";
    private const string ValidPhone = "11999999999";
    private const string ValidAddress = "Rua A, 1";
    private const string ValidName = "John Doe";

    private readonly IVehicleRepository _repo;
    private readonly IClientRepository _clientRepo;
    private readonly IUnitOfWork _uow;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        MappingConfig.Register();

        _repo = Substitute.For<IVehicleRepository>();
        _clientRepo = Substitute.For<IClientRepository>();
        _uow = Substitute.For<IUnitOfWork>();
        _service = new VehicleService(_repo, _clientRepo, _uow);
    }

    private static Client MakeClient()
        => new Client(ValidName, new TaxId(ValidCpf), ValidEmail, ValidPhone, ValidAddress);

    private static Vehicle MakeVehicle(Guid clientId)
        => new Vehicle(clientId, new LicensePlate("ABC-1234"), "Toyota", "Corolla", 2020);

    // -----------------------------------------------------------------------
    // CreateAsync — client not found returns failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenClientNotFound_ReturnsFailure()
    {
        var clientId = Guid.NewGuid();
        _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns((Client?)null);

        var req = new CreateVehicleRequest(clientId, "ABC-1234", "Toyota", "Corolla", 2020);
        var result = await _service.CreateAsync(req);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Client {clientId} not found.", result.Error);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenClientFound_ReturnsSuccessWithVehicleResponse()
    {
        var client = MakeClient();
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        var req = new CreateVehicleRequest(client.Id, "ABC-1234", "Toyota", "Corolla", 2020);
        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ABC1234", result.Value!.LicensePlate);
        Assert.Equal("Toyota", result.Value.Make);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Vehicle {id} not found.", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var clientId = Guid.NewGuid();
        var vehicle = MakeVehicle(clientId);
        _repo.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _service.GetByIdAsync(vehicle.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(vehicle.Id, result.Value!.Id);
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — pageSize clamped
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenPageSizeOver100_ClampsTo100()
    {
        var vehicles = new List<Vehicle>().AsReadOnly();
        _repo.GetAllAsync(1, 100, Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(((IReadOnlyList<Vehicle>)vehicles, 0)));

        var result = await _service.GetAllAsync(1, 500);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.PageSize);
        await _repo.Received(1).GetAllAsync(1, 100, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var req = new UpdateVehicleRequest("Honda", "Civic", 2022);
        var result = await _service.UpdateAsync(id, req);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Vehicle {id} not found.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndCallsCommitOnce()
    {
        var clientId = Guid.NewGuid();
        var vehicle = MakeVehicle(clientId);
        _repo.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var req = new UpdateVehicleRequest("Honda", "Civic", 2022);
        var result = await _service.UpdateAsync(vehicle.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Equal("Honda", result.Value!.Make);
        Assert.Equal("Civic", result.Value.Model);
        Assert.Equal(2022, result.Value.Year);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Vehicle {id} not found.", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ReturnsSuccessAndCallsCommitOnce()
    {
        var clientId = Guid.NewGuid();
        var vehicle = MakeVehicle(clientId);
        _repo.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _service.DeleteAsync(vehicle.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetByClientAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByClientAsync_ReturnsPagedResult()
    {
        var clientId = Guid.NewGuid();
        var client = MakeClient();
        _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);

        var vehicles = new List<Vehicle>().AsReadOnly();
        _repo.GetByClientAsync(clientId, 1, 20, Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(((IReadOnlyList<Vehicle>)vehicles, 0)));

        var result = await _service.GetByClientAsync(clientId, 1, 20);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalCount);
        await _repo.Received(1).GetByClientAsync(clientId, 1, 20, Arg.Any<CancellationToken>());
    }
}
