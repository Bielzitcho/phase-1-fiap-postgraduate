using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.Entities;

public class OrderedPart : Entity<Guid>
{
    public Guid PartId { get; private set; }
    public string PartName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }  // price snapshot — frozen at add-time
    public int Quantity { get; private set; }

    public OrderedPart(Guid partId, string partName, decimal unitPrice, int quantity)
        : base(Guid.NewGuid())
    {
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        PartId = partId;
        PartName = partName ?? throw new DomainException("Part name cannot be null.");
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    // EF Core parameterless constructor
    private OrderedPart() : base() { }
}
