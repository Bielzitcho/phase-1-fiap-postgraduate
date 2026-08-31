using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.Entities;

public class OrderedService : Entity<Guid>
{
    public Guid ServiceTypeId { get; private set; }
    public string ServiceTypeName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }  // price snapshot — frozen at add-time

    public OrderedService(Guid serviceTypeId, string serviceTypeName, decimal unitPrice)
        : base(Guid.NewGuid())
    {
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
        ServiceTypeId = serviceTypeId;
        ServiceTypeName = serviceTypeName ?? throw new DomainException("Service type name cannot be null.");
        UnitPrice = unitPrice;
    }

    // EF Core parameterless constructor
    private OrderedService() : base() { }
}
