using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.Aggregates;

public class VehicleUpdateTests
{
    private static Vehicle MakeVehicle()
        => new Vehicle(Guid.NewGuid(), new LicensePlate("ABC-1234"), "Toyota", "Corolla", 2020);

    // -----------------------------------------------------------------------
    // UpdateDetails — happy path
    // -----------------------------------------------------------------------

    [Fact]
    public void Vehicle_UpdateDetails_WithValidArgs_UpdatesMakeModelYear()
    {
        var vehicle = MakeVehicle();
        vehicle.UpdateDetails("Honda", "Civic", 2022);
        Assert.Equal("Honda", vehicle.Make);
        Assert.Equal("Civic", vehicle.Model);
        Assert.Equal(2022, vehicle.Year);
    }

    [Fact]
    public void Vehicle_UpdateDetails_LicensePlate_IsUnchanged()
    {
        var vehicle = MakeVehicle();
        vehicle.UpdateDetails("Honda", "Civic", 2022);
        Assert.Equal("ABC1234", vehicle.LicensePlate.Value);
    }

    // -----------------------------------------------------------------------
    // UpdateDetails — guard violations
    // -----------------------------------------------------------------------

    [Fact]
    public void Vehicle_UpdateDetails_WithEmptyMake_ThrowsDomainException()
    {
        var vehicle = MakeVehicle();
        var ex = Assert.Throws<DomainException>(() => vehicle.UpdateDetails("", "Civic", 2022));
        Assert.Equal("Vehicle make cannot be empty.", ex.Message);
    }

    [Fact]
    public void Vehicle_UpdateDetails_WithInvalidYear_ThrowsDomainException()
    {
        var vehicle = MakeVehicle();
        var ex = Assert.Throws<DomainException>(() => vehicle.UpdateDetails("Honda", "Civic", 1900));
        Assert.Contains("between 1901 and", ex.Message);
    }
}
