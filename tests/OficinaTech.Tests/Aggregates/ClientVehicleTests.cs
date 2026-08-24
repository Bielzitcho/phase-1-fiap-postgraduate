using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.Aggregates;

public class ClientVehicleTests
{
    private static TaxId ValidTaxId() => new("529.982.247-25");

    [Fact]
    public void Client_WithValidArguments_ShouldConstruct()
    {
        var taxId = ValidTaxId();
        var client = new Client("John Doe", taxId, "john@example.com", "11999999999");
        Assert.Equal("John Doe", client.Name);
        Assert.Equal(taxId, client.TaxId);
        Assert.Equal("john@example.com", client.Email);
        Assert.NotEqual(Guid.Empty, client.Id);
    }

    [Fact]
    public void Client_WithEmptyName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Client("", ValidTaxId(), "john@example.com", "11999999999"));
    }

    [Fact]
    public void Client_WithNullTaxId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Client("John Doe", null!, "john@example.com", "11999999999"));
    }

    [Fact]
    public void Vehicle_WithValidArguments_ShouldConstruct()
    {
        var licensePlate = new LicensePlate("ABC-1234");
        var clientId = Guid.NewGuid();
        var vehicle = new Vehicle(clientId, licensePlate, "Toyota", "Corolla", 2020);
        Assert.Equal(clientId, vehicle.ClientId);
        Assert.Equal("ABC1234", vehicle.LicensePlate.Value);
        Assert.NotEqual(Guid.Empty, vehicle.Id);
    }

    [Fact]
    public void Vehicle_WithEmptyClientId_ShouldThrowDomainException()
    {
        var licensePlate = new LicensePlate("ABC-1234");
        Assert.Throws<DomainException>(() =>
            new Vehicle(Guid.Empty, licensePlate, "Toyota", "Corolla", 2020));
    }
}
