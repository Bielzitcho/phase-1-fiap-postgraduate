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
}
