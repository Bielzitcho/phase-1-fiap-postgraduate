using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Domain.Aggregates;

public class Vehicle : AggregateRoot<Guid>
{
    public Guid ClientId { get; private set; }
    public LicensePlate LicensePlate { get; private set; }
    public string Make { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }

    public Vehicle(Guid clientId, LicensePlate licensePlate, string make, string model, int year)
        : base(Guid.NewGuid())
    {
        ClientId = clientId != Guid.Empty
            ? clientId
            : throw new DomainException("ClientId cannot be empty.");
        LicensePlate = licensePlate ?? throw new DomainException("LicensePlate is required.");
        Make = !string.IsNullOrWhiteSpace(make)
            ? make
            : throw new DomainException("Vehicle make cannot be empty.");
        Model = !string.IsNullOrWhiteSpace(model)
            ? model
            : throw new DomainException("Vehicle model cannot be empty.");
        Year = year > 1900
            ? year
            : throw new DomainException("Vehicle year must be valid.");
    }

    public void UpdateDetails(string make, string model, int year)
    {
        Make = !string.IsNullOrWhiteSpace(make)
            ? make
            : throw new DomainException("Vehicle make cannot be empty.");
        Model = !string.IsNullOrWhiteSpace(model)
            ? model
            : throw new DomainException("Vehicle model cannot be empty.");
        Year = year > 1900
            ? year
            : throw new DomainException("Vehicle year must be valid.");
    }

    private Vehicle() : base() { }
}
