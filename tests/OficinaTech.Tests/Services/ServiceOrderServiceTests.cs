using NSubstitute;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Application.Mapping;
using OficinaTech.Application.Services;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.Services;

public class ServiceOrderServiceTests
{
    private readonly IServiceOrderRepository _repo;
    private readonly IClientRepository _clientRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IServiceTypeRepository _serviceTypeRepo;
    private readonly IPartRepository _partRepo;
    private readonly IUnitOfWork _uow;
    private readonly ServiceOrderService _service;

    private const string ValidCpf = "529.982.247-25";

    public ServiceOrderServiceTests()
    {
        MappingConfig.Register();

        _repo = Substitute.For<IServiceOrderRepository>();
        _clientRepo = Substitute.For<IClientRepository>();
        _vehicleRepo = Substitute.For<IVehicleRepository>();
        _serviceTypeRepo = Substitute.For<IServiceTypeRepository>();
        _partRepo = Substitute.For<IPartRepository>();
        _uow = Substitute.For<IUnitOfWork>();

        _service = new ServiceOrderService(
            _repo, _clientRepo, _vehicleRepo, _serviceTypeRepo, _partRepo, _uow);
    }

    // -----------------------------------------------------------------------
    // Helper factories
    // -----------------------------------------------------------------------

    private static Client MakeClient()
        => new Client(
            "Joao Silva",
            new TaxId(ValidCpf),
            "joao@example.com",
            "11999999999",
            "Rua das Flores, 123");

    private static Vehicle MakeVehicle(Guid clientId)
        => new Vehicle(
            clientId,
            new LicensePlate("ABC-1234"),
            "Toyota",
            "Corolla",
            2020);

    private static ServiceType MakeServiceType()
        => new ServiceType("Troca de oleo", 150m, "Troca completa de oleo");

    private static Part MakePart()
        => new Part("Filtro de oleo", 50m, 100, "Filtro original");

    // -----------------------------------------------------------------------
    // CreateAsync — success path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithValidTaxIdAndVehicleAndItems_ReturnsSuccessWithCorrectTotalAmount()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var serviceType = MakeServiceType();
        var part = MakePart();

        var vehicleId = vehicle.Id;

        _clientRepo.GetByTaxIdAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(vehicle);
        _serviceTypeRepo.GetByIdAsync(serviceType.Id, Arg.Any<CancellationToken>())
            .Returns(serviceType);
        _partRepo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>())
            .Returns(part);

        var req = new CreateServiceOrderRequest(
            TaxId: ValidCpf,
            VehicleId: vehicleId,
            Services: [new AddServiceRequest(serviceType.Id)],
            Parts: [new AddPartRequest(part.Id, 2)]);

        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // TotalAmount = serviceType.BasePrice + part.UnitPrice * quantity = 150 + 50*2 = 250
        Assert.Equal(250m, result.Value!.TotalAmount);
        Assert.Equal("Recebida", result.Value.Status);
    }

    // -----------------------------------------------------------------------
    // CreateAsync — client not found returns Failure (D-02)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithUnknownTaxId_ReturnsFailureWithNotFoundMessage()
    {
        _clientRepo.GetByTaxIdAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var req = new CreateServiceOrderRequest(
            TaxId: ValidCpf,
            VehicleId: Guid.NewGuid());

        var result = await _service.CreateAsync(req);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CreateAsync — vehicle not found returns Failure (OS-02)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithUnknownVehicleId_ReturnsFailure()
    {
        var client = MakeClient();
        var vehicleId = Guid.NewGuid();

        _clientRepo.GetByTaxIdAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns((Vehicle?)null);

        var req = new CreateServiceOrderRequest(
            TaxId: ValidCpf,
            VehicleId: vehicleId);

        var result = await _service.CreateAsync(req);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    // -----------------------------------------------------------------------
    // CreateAsync — services and parts loaded, CommitAsync called exactly once
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithServicesAndParts_CallsCommitExactlyOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var serviceType = MakeServiceType();
        var part = MakePart();
        var vehicleId = vehicle.Id;

        _clientRepo.GetByTaxIdAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(vehicle);
        _serviceTypeRepo.GetByIdAsync(serviceType.Id, Arg.Any<CancellationToken>())
            .Returns(serviceType);
        _partRepo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>())
            .Returns(part);

        var req = new CreateServiceOrderRequest(
            TaxId: ValidCpf,
            VehicleId: vehicleId,
            Services: [new AddServiceRequest(serviceType.Id)],
            Parts: [new AddPartRequest(part.Id, 3)]);

        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // CreateAsync — null services and parts creates shell OS with TotalAmount 0 (D-03)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithNullServicesAndParts_CreatesShellOsWithTotalAmountZero()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var vehicleId = vehicle.Id;

        _clientRepo.GetByTaxIdAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(vehicle);

        var req = new CreateServiceOrderRequest(
            TaxId: ValidCpf,
            VehicleId: vehicleId,
            Services: null,
            Parts: null);

        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.TotalAmount);
        Assert.Equal("Recebida", result.Value.Status);
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync — not found returns Failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenOrderNotFound_ReturnsFailureWithNotFoundMessage()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdWithIncludesAsync(id, Arg.Any<CancellationToken>())
            .Returns((ServiceOrder?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
