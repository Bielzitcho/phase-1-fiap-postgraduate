using OficinaTech.Domain.Entities;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.Aggregates;

public class ServiceOrder : AggregateRoot<Guid>
{
    public ServiceOrderStatus Status { get; private set; }  // MUST be private set (SC-2, OS-08)
    public Guid ClientId { get; private set; }
    public Guid VehicleId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? FinalizationDate { get; private set; }

    private readonly List<OrderedService> _orderedServices = new();
    private readonly List<OrderedPart> _orderedParts = new();

    public IReadOnlyCollection<OrderedService> OrderedServices => _orderedServices.AsReadOnly();
    public IReadOnlyCollection<OrderedPart> OrderedParts => _orderedParts.AsReadOnly();

    // D-05: computed property — no stored field, no Budget VO
    public decimal TotalAmount =>
        _orderedServices.Sum(s => s.UnitPrice) +
        _orderedParts.Sum(p => p.UnitPrice * p.Quantity);

    public ServiceOrder(Guid clientId, Guid vehicleId) : base(Guid.NewGuid())
    {
        ClientId = clientId != Guid.Empty
            ? clientId
            : throw new DomainException("ClientId is required.");
        VehicleId = vehicleId != Guid.Empty
            ? vehicleId
            : throw new DomainException("VehicleId is required.");
        Status = ServiceOrderStatus.Recebida;
        CreatedAt = DateTime.UtcNow;
    }

    // EF Core parameterless constructor
    private ServiceOrder() : base() { }

    // D-06: status guard — only Recebida and EmDiagnostico allow modification
    public void AddService(Guid serviceTypeId, string serviceTypeName, decimal unitPrice)
    {
        GuardAgainstLockedStatus();
        _orderedServices.Add(new OrderedService(serviceTypeId, serviceTypeName, unitPrice));
    }

    public void AddPart(Guid partId, string partName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Part quantity must be greater than zero.");
        GuardAgainstLockedStatus();
        _orderedParts.Add(new OrderedPart(partId, partName, unitPrice, quantity));
    }

    private void GuardAgainstLockedStatus()
    {
        if (Status != ServiceOrderStatus.Recebida && Status != ServiceOrderStatus.EmDiagnostico)
            throw new DomainException(
                $"Cannot modify ServiceOrder in '{Status}' status. Allowed only in Recebida or EmDiagnostico.");
    }

    // D-07: six named transition methods — one status change each

    public void StartDiagnosis()
    {
        if (Status != ServiceOrderStatus.Recebida)
            throw new DomainException($"Cannot start diagnosis: order is in '{Status}' status.");
        Status = ServiceOrderStatus.EmDiagnostico;
    }

    public void SendForApproval()
    {
        if (Status != ServiceOrderStatus.EmDiagnostico)
            throw new DomainException($"Cannot send for approval: order is in '{Status}' status.");
        Status = ServiceOrderStatus.AguardandoAprovacao;
    }

    public void Approve()
    {
        if (Status != ServiceOrderStatus.AguardandoAprovacao)
            throw new DomainException($"Cannot approve: order is in '{Status}' status.");
        Status = ServiceOrderStatus.EmExecucao;
    }

    // Per RESEARCH Open Question 1 / A4 resolution: Approve() owns the AguardandoAprovacao → EmExecucao
    // transition. StartExecution() is retained as a named idempotent guard that only succeeds while
    // already in EmExecucao and performs no status change, so all six D-07 method names exist.
    public void StartExecution()
    {
        if (Status != ServiceOrderStatus.EmExecucao)
            throw new DomainException($"Cannot start execution: order is in '{Status}' status. Call Approve() first.");
        // Idempotent guard: order is already in EmExecucao — no status change required.
    }

    public void Finalize()
    {
        if (Status != ServiceOrderStatus.EmExecucao)
            throw new DomainException($"Cannot finalize: order is in '{Status}' status.");
        Status = ServiceOrderStatus.Finalizada;
        FinalizationDate = DateTime.UtcNow;
        // D-11: caller (Application layer) is responsible for calling
        // serviceType.RecordExecution(FinalizationDate.Value - CreatedAt)
        // for each ordered service. Domain raises event in Phase 4.
    }

    public void MarkDelivered()
    {
        if (Status != ServiceOrderStatus.Finalizada)
            throw new DomainException($"Cannot mark as delivered: order is in '{Status}' status.");
        Status = ServiceOrderStatus.Entregue;
    }
}
