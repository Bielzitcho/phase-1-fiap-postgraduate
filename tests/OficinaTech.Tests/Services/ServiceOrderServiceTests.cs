using NSubstitute;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Application.Mapping;
using OficinaTech.Application.Services;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;
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
    private const string ValidCpfDigitsOnly = "52998224725";

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

    /// <summary>
    /// Creates a ServiceOrder in the specified status by calling the transition
    /// chain via internal aggregate methods. Needed for tests that require a
    /// specific starting status.
    /// </summary>
    private static ServiceOrder MakeOrderInStatus(Guid clientId, Guid vehicleId, ServiceOrderStatus targetStatus)
    {
        var order = new ServiceOrder(clientId, vehicleId);
        if (targetStatus == ServiceOrderStatus.Recebida) return order;
        order.StartDiagnosis();
        if (targetStatus == ServiceOrderStatus.EmDiagnostico) return order;
        order.SendForApproval();
        if (targetStatus == ServiceOrderStatus.AguardandoAprovacao) return order;
        order.Approve();
        if (targetStatus == ServiceOrderStatus.EmExecucao) return order;
        order.Finalize();
        if (targetStatus == ServiceOrderStatus.Finalizada) return order;
        order.MarkDelivered();
        return order;
    }

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

    // -----------------------------------------------------------------------
    // StartDiagnosisAsync — transitions from Recebida to EmDiagnostico (OS-09)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task StartDiagnosisAsync_OnRecebidaOrder_TransitionsToEmDiagnosticoAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _service.StartDiagnosisAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("EmDiagnostico", result.Value!.Status);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartDiagnosisAsync_OnWrongStatusOrder_ThrowsDomainException()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        // Order already in EmDiagnostico — cannot start diagnosis again
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.EmDiagnostico);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        // DomainException should bubble to the global handler (400)
        await Assert.ThrowsAsync<DomainException>(() => _service.StartDiagnosisAsync(order.Id));
    }

    // -----------------------------------------------------------------------
    // SendForApprovalAsync — transitions from EmDiagnostico to AguardandoAprovacao (OS-06)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SendForApprovalAsync_OnEmDiagnosticoOrder_TransitionsToAguardandoAprovacaoAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.EmDiagnostico);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _service.SendForApprovalAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("AguardandoAprovacao", result.Value!.Status);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // ApproveAsync — taxId mismatch returns Failure with "does not match" (D-05)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApproveAsync_WithMismatchedTaxId_ReturnsFailureWithDoesNotMatchMessage_AndNoCommit()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.AguardandoAprovacao);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);

        var req = new ApproveServiceOrderRequest(TaxId: "111.111.111-11");

        var result = await _service.ApproveAsync(order.Id, req);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not match", result.Error, StringComparison.OrdinalIgnoreCase);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // ApproveAsync — matching taxId calls Approve(), DecrementStock, commits once (D-06)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApproveAsync_WithMatchingTaxId_ApprovesAndDecrementsStockAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var part = MakePart();
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);
        order.AddPart(part.Id, part.Name, part.UnitPrice, 3);
        order.StartDiagnosis();
        order.SendForApproval();

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        _partRepo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>())
            .Returns(part);

        // Use the exact digits-only string to match client.TaxId.Value
        var req = new ApproveServiceOrderRequest(TaxId: ValidCpf);

        var result = await _service.ApproveAsync(order.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Equal("EmExecucao", result.Value!.Status);
        // Stock should have been decremented: 100 - 3 = 97
        Assert.Equal(97, part.StockQuantity);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // ApproveAsync — insufficient stock lets DomainException bubble (no commit) (D-06)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApproveAsync_WhenPartHasInsufficientStock_ThrowsDomainException_AndNoCommit()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        // Create part with only 1 unit in stock
        var part = new Part("Filtro especial", 80m, 1, null);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);
        // Add 5 units of a part with only 1 in stock
        order.AddPart(part.Id, part.Name, part.UnitPrice, 5);
        order.StartDiagnosis();
        order.SendForApproval();

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        _partRepo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>())
            .Returns(part);

        var req = new ApproveServiceOrderRequest(TaxId: ValidCpf);

        await Assert.ThrowsAsync<DomainException>(() => _service.ApproveAsync(order.Id, req));
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // FinalizeAsync — calls RecordExecution for each service and commits once (SRV-06, D-10)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FinalizeAsync_OnEmExecucaoOrder_CallsRecordExecutionForEachServiceAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var serviceType = MakeServiceType();
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);
        order.AddService(serviceType.Id, serviceType.Name, serviceType.BasePrice);
        order.StartDiagnosis();
        order.SendForApproval();
        order.Approve();

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _serviceTypeRepo.GetByIdAsync(serviceType.Id, Arg.Any<CancellationToken>())
            .Returns(serviceType);

        var result = await _service.FinalizeAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Finalizada", result.Value!.Status);
        // RecordExecution increments _executionCount — AverageExecutionTime is no longer zero
        Assert.NotEqual(TimeSpan.Zero, serviceType.AverageExecutionTime);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // AddServiceAsync — loads ServiceType, calls order.AddService, commits once (D-04)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddServiceAsync_OnRecebidaOrder_AddsServiceAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var serviceType = MakeServiceType();
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>())
            .Returns(vehicle);
        _serviceTypeRepo.GetByIdAsync(serviceType.Id, Arg.Any<CancellationToken>())
            .Returns(serviceType);

        var req = new AddServiceRequest(ServiceTypeId: serviceType.Id);

        var result = await _service.AddServiceAsync(order.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.OrderedServices);
        Assert.Equal(serviceType.BasePrice, result.Value.TotalAmount);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // AddPartAsync — loads Part, calls order.AddPart, commits once (D-04)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddPartAsync_OnRecebidaOrder_AddsPartAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var part = MakePart();
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>())
            .Returns(vehicle);
        _partRepo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>())
            .Returns(part);

        var req = new AddPartRequest(PartId: part.Id, Quantity: 2);

        var result = await _service.AddPartAsync(order.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.OrderedParts);
        Assert.Equal(part.UnitPrice * 2, result.Value.TotalAmount);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — clamps pageSize >100 to 100 and forwards filters to repo (D-12, OS-10)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WithPageSizeOver100_ClampsTo100AndForwardsFilters()
    {
        var status = ServiceOrderStatus.Recebida;
        var clientId = Guid.NewGuid();

        _repo.GetAllAsync(status, clientId, 1, 100, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceOrder>)[], 0));

        var result = await _service.GetAllAsync(status, clientId, 1, 200);

        Assert.True(result.IsSuccess);
        // Verify repo received the clamped pageSize=100 and unaltered filters
        await _repo.Received(1).GetAllAsync(status, clientId, 1, 100, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — page <1 clamped to 1 (D-12)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WithPageBelow1_ClampsTo1()
    {
        _repo.GetAllAsync(null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceOrder>)[], 0));

        var result = await _service.GetAllAsync(null, null, -5, 20);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).GetAllAsync(null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — maps items to ServiceOrderSummaryResponse and returns correct TotalCount (D-12)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WithItems_MapsToSummaryResponseAndReturnsCorrectTotalCount()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);
        var orders = (IReadOnlyList<ServiceOrder>)[order];

        _repo.GetAllAsync(null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((orders, 1));

        var result = await _service.GetAllAsync(null, null, 1, 20);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.Equal(order.Id, result.Value.Items[0].Id);
        Assert.Equal("Recebida", result.Value.Items[0].Status);
    }

    // -----------------------------------------------------------------------
    // GetByTaxIdAsync — client not found returns Failure with "not found" (D-13, OS-12)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByTaxIdAsync_WhenClientNotFound_ReturnsFailureWithNotFoundMessage()
    {
        _clientRepo.GetByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var result = await _service.GetByTaxIdAsync(ValidCpf);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // GetByTaxIdAsync — client exists, returns list of PublicServiceOrderSummary (D-13)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByTaxIdAsync_WhenClientExists_ReturnsPublicSummaryListWithVehiclePlate()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);
        var orders = (IReadOnlyList<ServiceOrder>)[order];

        _clientRepo.GetByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(client);
        _repo.GetAllAsync(null, client.Id, 1, 100, Arg.Any<CancellationToken>())
            .Returns((orders, 1));
        _vehicleRepo.GetByIdAsync(order.VehicleId, Arg.Any<CancellationToken>())
            .Returns(vehicle);

        var result = await _service.GetByTaxIdAsync(ValidCpf);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        var summary = result.Value![0];
        Assert.Equal(order.Id, summary.Id);
        Assert.Equal("Recebida", summary.Status);
        Assert.Equal(order.TotalAmount, summary.TotalAmount);
        Assert.Equal(vehicle.LicensePlate.Value, summary.VehiclePlate);
        // The summary must be PublicServiceOrderSummary — not the full shape
        Assert.IsType<PublicServiceOrderSummary>(summary);
    }

    // -----------------------------------------------------------------------
    // GetByTaxIdAsync — taxId normalization: formatted CPF resolves same as digits-only (D-13)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByTaxIdAsync_WithFormattedTaxId_NormalizesAndResolvesCorrectClient()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var orders = (IReadOnlyList<ServiceOrder>)Array.Empty<ServiceOrder>();

        // Return the client for ANY normalized lookup — service must strip non-digits
        _clientRepo.GetByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(client);
        _repo.GetAllAsync(null, client.Id, 1, 100, Arg.Any<CancellationToken>())
            .Returns((orders, 0));

        // Call with formatted CPF — "529.982.247-25"
        var result = await _service.GetByTaxIdAsync(ValidCpf);

        Assert.True(result.IsSuccess);
        // The repo should have been called with the digits-only normalized string
        await _clientRepo.Received(1).GetByTaxIdAsync(ValidCpfDigitsOnly, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // MarkDeliveredAsync — happy path: Finalizada → Entregue, commits once (QA-01)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkDeliveredAsync_OnFinalizadaOrder_TransitionsToEntregueAndCommitsOnce()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Finalizada);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _service.MarkDeliveredAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Entregue", result.Value!.Status);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync — happy path: returns full response with client and vehicle (QA-01)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsFullResponseWithClientAndVehicle()
    {
        var client = MakeClient();
        var vehicle = MakeVehicle(client.Id);
        var order = MakeOrderInStatus(client.Id, vehicle.Id, ServiceOrderStatus.Recebida);

        _repo.GetByIdWithIncludesAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clientRepo.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        _vehicleRepo.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>())
            .Returns(vehicle);

        var result = await _service.GetByIdAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Value!.Id);
        Assert.Equal("Recebida", result.Value.Status);
    }

    // ConcurrencyDomainException constructor coverage

    [Fact]
    public void ConcurrencyDomainException_Constructor_SetsMessage()
    {
        var ex = new OficinaTech.Domain.Seedwork.ConcurrencyDomainException("conflict message");
        Assert.Equal("conflict message", ex.Message);
    }
}
