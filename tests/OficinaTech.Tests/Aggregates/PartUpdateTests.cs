using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Seedwork;
using Xunit;

namespace OficinaTech.Tests.Aggregates;

public class PartUpdateTests
{
    private static Part MakePart()
        => new Part("Spark Plug", 15.99m, 50);

    // -----------------------------------------------------------------------
    // Constructor — optional Description
    // -----------------------------------------------------------------------

    [Fact]
    public void Part_Constructor_WithDescription_SetsDescription()
    {
        var part = new Part("Spark Plug", 15.99m, 50, "NGK iridium spark plug");
        Assert.Equal("NGK iridium spark plug", part.Description);
    }

    [Fact]
    public void Part_Constructor_WithoutDescription_LeavesDescriptionNull()
    {
        var part = MakePart();
        Assert.Null(part.Description);
    }

    // -----------------------------------------------------------------------
    // UpdateDetails — happy path
    // -----------------------------------------------------------------------

    [Fact]
    public void Part_UpdateDetails_WithValidArgs_UpdatesAllFields()
    {
        var part = MakePart();
        part.UpdateDetails("Oil Filter", 25.00m, 100, "Heavy-duty oil filter");
        Assert.Equal("Oil Filter", part.Name);
        Assert.Equal(25.00m, part.UnitPrice);
        Assert.Equal(100, part.StockQuantity);
        Assert.Equal("Heavy-duty oil filter", part.Description);
    }

    [Fact]
    public void Part_UpdateDetails_WithNullDescription_SetsDescriptionNull()
    {
        var part = new Part("Spark Plug", 15.99m, 50, "Initial description");
        part.UpdateDetails("Spark Plug", 15.99m, 50, null);
        Assert.Null(part.Description);
    }

    // -----------------------------------------------------------------------
    // UpdateDetails — guard violations
    // -----------------------------------------------------------------------

    [Fact]
    public void Part_UpdateDetails_WithEmptyName_ThrowsDomainException()
    {
        var part = MakePart();
        var ex = Assert.Throws<DomainException>(() => part.UpdateDetails("", 15.99m, 50));
        Assert.Equal("Part name cannot be empty.", ex.Message);
    }

    [Fact]
    public void Part_UpdateDetails_WithZeroUnitPrice_ThrowsDomainException()
    {
        var part = MakePart();
        var ex = Assert.Throws<DomainException>(() => part.UpdateDetails("Spark Plug", 0m, 50));
        Assert.Equal("Unit price must be greater than zero.", ex.Message);
    }

    [Fact]
    public void Part_UpdateDetails_WithNegativeStock_ThrowsDomainException()
    {
        var part = MakePart();
        var ex = Assert.Throws<DomainException>(() => part.UpdateDetails("Spark Plug", 15.99m, -1));
        Assert.Equal("Stock quantity cannot be negative.", ex.Message);
    }
}
