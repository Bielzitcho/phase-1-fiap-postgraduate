using System.Text.RegularExpressions;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Services;

public class ServiceOrderService : IServiceOrderService
{
    private readonly IServiceOrderRepository _repo;
    private readonly IClientRepository _clientRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IServiceTypeRepository _serviceTypeRepo;
    private readonly IPartRepository _partRepo;
    private readonly IUnitOfWork _uow;

    public ServiceOrderService(
        IServiceOrderRepository repo,
        IClientRepository clientRepo,
        IVehicleRepository vehicleRepo,
        IServiceTypeRepository serviceTypeRepo,
        IPartRepository partRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _clientRepo = clientRepo;
        _vehicleRepo = vehicleRepo;
        _serviceTypeRepo = serviceTypeRepo;
        _partRepo = partRepo;
        _uow = uow;
    }

    public async Task<Result<ServiceOrderResponse>> CreateAsync(
        CreateServiceOrderRequest req, CancellationToken ct = default)
    {
        // D-02: Resolve client by taxId
        var client = await _clientRepo.GetByTaxIdAsync(req.TaxId, ct);
        if (client is null)
            return Result<ServiceOrderResponse>.Failure($"Client with taxId {req.TaxId} not found.");

        // OS-02: Validate vehicle exists
        var vehicle = await _vehicleRepo.GetByIdAsync(req.VehicleId, ct);
        if (vehicle is null)
            return Result<ServiceOrderResponse>.Failure($"Vehicle {req.VehicleId} not found.");

        // D-01: One-shot create — build aggregate in memory
        var order = new ServiceOrder(client.Id, req.VehicleId);

        // OS-03: Add services
        foreach (var item in req.Services ?? [])
        {
            var serviceType = await _serviceTypeRepo.GetByIdAsync(item.ServiceTypeId, ct);
            if (serviceType is null)
                return Result<ServiceOrderResponse>.Failure($"ServiceType {item.ServiceTypeId} not found.");
            order.AddService(serviceType.Id, serviceType.Name, serviceType.BasePrice);
        }

        // OS-04: Add parts
        foreach (var item in req.Parts ?? [])
        {
            var part = await _partRepo.GetByIdAsync(item.PartId, ct);
            if (part is null)
                return Result<ServiceOrderResponse>.Failure($"Part {item.PartId} not found.");
            order.AddPart(part.Id, part.Name, part.UnitPrice, item.Quantity);
        }

        await _repo.AddAsync(order, ct);
        await _uow.CommitAsync(ct);  // Single CommitAsync per D-01

        // Project response — manual projection to avoid Mapster nested VO pitfalls
        var response = ProjectResponse(order, client, vehicle);
        return Result<ServiceOrderResponse>.Success(response);
    }

    public async Task<Result<ServiceOrderResponse>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        // D-11: Load with OrderedServices and OrderedParts included
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderResponse>.Failure($"ServiceOrder {id} not found.");

        // Load client and vehicle for embedded summaries
        var client = await _clientRepo.GetByIdAsync(order.ClientId, ct);
        if (client is null)
            return Result<ServiceOrderResponse>.Failure($"Client {order.ClientId} not found.");

        var vehicle = await _vehicleRepo.GetByIdAsync(order.VehicleId, ct);
        if (vehicle is null)
            return Result<ServiceOrderResponse>.Failure($"Vehicle {order.VehicleId} not found.");

        var response = ProjectResponse(order, client, vehicle);
        return Result<ServiceOrderResponse>.Success(response);
    }

    // -----------------------------------------------------------------------
    // Status lifecycle transitions (OS-09: only via aggregate methods, no Status= assignment)
    // -----------------------------------------------------------------------

    public async Task<Result<ServiceOrderSummaryResponse>> StartDiagnosisAsync(
        Guid id, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderSummaryResponse>.Failure($"ServiceOrder {id} not found.");

        order.StartDiagnosis();  // throws DomainException on wrong status — bubbles to 400 handler

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);

        return Result<ServiceOrderSummaryResponse>.Success(ProjectSummary(order));
    }

    public async Task<Result<ServiceOrderSummaryResponse>> SendForApprovalAsync(
        Guid id, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderSummaryResponse>.Failure($"ServiceOrder {id} not found.");

        order.SendForApproval();  // OS-06: moves to AguardandoAprovacao

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);

        return Result<ServiceOrderSummaryResponse>.Success(ProjectSummary(order));
    }

    // D-05 + D-06: public approval with taxId ownership check and transactional stock decrement
    public async Task<Result<ServiceOrderSummaryResponse>> ApproveAsync(
        Guid id, ApproveServiceOrderRequest req, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderSummaryResponse>.Failure($"ServiceOrder {id} not found.");

        // D-05: taxId ownership check
        var client = await _clientRepo.GetByIdAsync(order.ClientId, ct);
        if (client is null)
            return Result<ServiceOrderSummaryResponse>.Failure($"Client {order.ClientId} not found.");

        // Normalize both sides to digits-only for comparison
        var normalizedSupplied = Regex.Replace(req.TaxId ?? string.Empty, @"\D", "");
        var normalizedClient = Regex.Replace(client.TaxId.Value ?? string.Empty, @"\D", "");

        if (normalizedSupplied != normalizedClient)
            return Result<ServiceOrderSummaryResponse>.Failure(
                "Provided taxId does not match the order's client.");

        // Transition the order
        order.Approve();

        // D-06: decrement stock for each ordered part — throws DomainException if insufficient
        foreach (var op in order.OrderedParts)
        {
            var part = await _partRepo.GetByIdAsync(op.PartId, ct);
            if (part is null)
                return Result<ServiceOrderSummaryResponse>.Failure($"Part {op.PartId} not found.");

            part.DecrementStock(op.Quantity);  // throws DomainException on insufficient stock
            await _partRepo.UpdateAsync(part, ct);
        }

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);  // Single commit — atomic D-06

        return Result<ServiceOrderSummaryResponse>.Success(ProjectSummary(order));
    }

    // D-10 + SRV-06: finalize and record execution time for each service type
    public async Task<Result<ServiceOrderSummaryResponse>> FinalizeAsync(
        Guid id, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderSummaryResponse>.Failure($"ServiceOrder {id} not found.");

        order.Finalize();  // sets FinalizationDate, transitions to Finalizada

        // SRV-06: update RecordExecution for each ordered service type
        var duration = order.FinalizationDate!.Value - order.CreatedAt;
        foreach (var os in order.OrderedServices)
        {
            var serviceType = await _serviceTypeRepo.GetByIdAsync(os.ServiceTypeId, ct);
            if (serviceType is not null)
            {
                serviceType.RecordExecution(duration);
                await _serviceTypeRepo.UpdateAsync(serviceType, ct);
            }
        }

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);  // Single commit — atomic D-10

        return Result<ServiceOrderSummaryResponse>.Success(ProjectSummary(order));
    }

    public async Task<Result<ServiceOrderSummaryResponse>> MarkDeliveredAsync(
        Guid id, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderSummaryResponse>.Failure($"ServiceOrder {id} not found.");

        order.MarkDelivered();

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);

        return Result<ServiceOrderSummaryResponse>.Success(ProjectSummary(order));
    }

    // -----------------------------------------------------------------------
    // Incremental item addition — D-04
    // -----------------------------------------------------------------------

    public async Task<Result<ServiceOrderResponse>> AddServiceAsync(
        Guid id, AddServiceRequest req, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderResponse>.Failure($"ServiceOrder {id} not found.");

        var serviceType = await _serviceTypeRepo.GetByIdAsync(req.ServiceTypeId, ct);
        if (serviceType is null)
            return Result<ServiceOrderResponse>.Failure($"ServiceType {req.ServiceTypeId} not found.");

        // GuardAgainstLockedStatus() is called inside AddService — throws DomainException if past EmDiagnostico
        order.AddService(serviceType.Id, serviceType.Name, serviceType.BasePrice);

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);

        // Load client and vehicle for full detail projection
        var client = await _clientRepo.GetByIdAsync(order.ClientId, ct);
        var vehicle = await _vehicleRepo.GetByIdAsync(order.VehicleId, ct);

        if (client is null)
            return Result<ServiceOrderResponse>.Failure($"Client {order.ClientId} not found.");
        if (vehicle is null)
            return Result<ServiceOrderResponse>.Failure($"Vehicle {order.VehicleId} not found.");

        return Result<ServiceOrderResponse>.Success(ProjectResponse(order, client, vehicle));
    }

    public async Task<Result<ServiceOrderResponse>> AddPartAsync(
        Guid id, AddPartRequest req, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithIncludesAsync(id, ct);
        if (order is null)
            return Result<ServiceOrderResponse>.Failure($"ServiceOrder {id} not found.");

        var part = await _partRepo.GetByIdAsync(req.PartId, ct);
        if (part is null)
            return Result<ServiceOrderResponse>.Failure($"Part {req.PartId} not found.");

        // GuardAgainstLockedStatus() is called inside AddPart — throws DomainException if past EmDiagnostico
        order.AddPart(part.Id, part.Name, part.UnitPrice, req.Quantity);

        await _repo.UpdateAsync(order, ct);
        await _uow.CommitAsync(ct);

        // Load client and vehicle for full detail projection
        var client = await _clientRepo.GetByIdAsync(order.ClientId, ct);
        var vehicle = await _vehicleRepo.GetByIdAsync(order.VehicleId, ct);

        if (client is null)
            return Result<ServiceOrderResponse>.Failure($"Client {order.ClientId} not found.");
        if (vehicle is null)
            return Result<ServiceOrderResponse>.Failure($"Vehicle {order.VehicleId} not found.");

        return Result<ServiceOrderResponse>.Success(ProjectResponse(order, client, vehicle));
    }

    // -----------------------------------------------------------------------
    // Projection helpers
    // -----------------------------------------------------------------------

    // Manual projection — D-11: nested VO mapping (TaxId.Value, LicensePlate.Value)
    private static ServiceOrderResponse ProjectResponse(ServiceOrder order, Client client, Vehicle vehicle)
    {
        var clientSummary = new ClientSummary(client.Id, client.Name, client.TaxId.Value);
        var vehicleSummary = new VehicleSummary(
            vehicle.Id, vehicle.LicensePlate.Value, vehicle.Make, vehicle.Model, vehicle.Year);

        var orderedServices = order.OrderedServices
            .Select(os => new OrderedServiceDto(os.ServiceTypeId, os.ServiceTypeName, os.UnitPrice))
            .ToList()
            .AsReadOnly();

        var orderedParts = order.OrderedParts
            .Select(op => new OrderedPartDto(op.PartId, op.PartName, op.UnitPrice, op.Quantity))
            .ToList()
            .AsReadOnly();

        return new ServiceOrderResponse(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt,
            order.FinalizationDate,
            clientSummary,
            vehicleSummary,
            orderedServices,
            orderedParts);
    }

    // Summary projection for transition endpoints — lightweight
    private static ServiceOrderSummaryResponse ProjectSummary(ServiceOrder order)
        => new ServiceOrderSummaryResponse(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt,
            order.ClientId,
            order.VehicleId);
}
