using System.ComponentModel.DataAnnotations;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.Aggregates;

public class Part : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public decimal UnitPrice { get; private set; }
    [ConcurrencyCheck]
    public int StockQuantity { get; private set; }
    public string? Description { get; private set; }

    public Part(string name, decimal unitPrice, int stockQuantity, string? description = null) : base(Guid.NewGuid())
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Part name cannot be empty.");
        UnitPrice = unitPrice > 0
            ? unitPrice
            : throw new DomainException("Unit price must be greater than zero.");
        StockQuantity = stockQuantity >= 0
            ? stockQuantity
            : throw new DomainException("Stock quantity cannot be negative.");
        Description = description;
    }

    public void UpdateDetails(string name, decimal unitPrice, int stockQuantity, string? description = null)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Part name cannot be empty.");
        UnitPrice = unitPrice > 0
            ? unitPrice
            : throw new DomainException("Unit price must be greater than zero.");
        StockQuantity = stockQuantity >= 0
            ? stockQuantity
            : throw new DomainException("Stock quantity cannot be negative.");
        Description = description;
    }

    public void DecrementStock(int qty)
    {
        if (qty > StockQuantity)
            throw new DomainException(
                $"Insufficient stock for part '{Name}'. Available: {StockQuantity}, requested: {qty}.");
        StockQuantity -= qty;
    }

    private Part() : base() { }
}
