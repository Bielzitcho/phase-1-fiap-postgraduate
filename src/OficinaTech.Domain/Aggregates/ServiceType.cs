using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.Aggregates;

public class ServiceType : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public decimal BasePrice { get; private set; }
    public string? Description { get; private set; }

    private int _executionCount;
    private double _totalExecutionMinutes;

    public TimeSpan AverageExecutionTime => _executionCount == 0
        ? TimeSpan.Zero
        : TimeSpan.FromMinutes(_totalExecutionMinutes / _executionCount);

    public ServiceType(string name, decimal basePrice, string? description = null) : base(Guid.NewGuid())
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Service type name cannot be empty.");
        BasePrice = basePrice > 0
            ? basePrice
            : throw new DomainException("Base price must be greater than zero.");
        Description = description;
    }

    private ServiceType() : base() { }

    // D-10: called when ServiceOrder transitions to Finalizada (D-11)
    public void RecordExecution(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new DomainException("Execution duration cannot be negative.");
        _executionCount++;
        _totalExecutionMinutes += duration.TotalMinutes;
    }

    public void UpdateBasePrice(decimal newPrice)
    {
        BasePrice = newPrice > 0
            ? newPrice
            : throw new DomainException("Base price must be greater than zero.");
    }

    public void UpdateName(string name)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Service type name cannot be empty.");
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}
