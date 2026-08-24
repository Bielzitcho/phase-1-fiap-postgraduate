using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Seedwork;
using Xunit;

namespace OficinaTech.Tests.Aggregates;

public class ServiceTypeDescriptionTests
{
    // -----------------------------------------------------------------------
    // Constructor — optional Description
    // -----------------------------------------------------------------------

    [Fact]
    public void ServiceType_Constructor_WithDescription_SetsDescription()
    {
        var st = new ServiceType("Oil Change", 100m, "Full synthetic oil change");
        Assert.Equal("Full synthetic oil change", st.Description);
    }

    [Fact]
    public void ServiceType_Constructor_WithoutDescription_LeavesDescriptionNull()
    {
        var st = new ServiceType("Oil Change", 100m);
        Assert.Null(st.Description);
    }

    // -----------------------------------------------------------------------
    // UpdateName
    // -----------------------------------------------------------------------

    [Fact]
    public void ServiceType_UpdateName_WithValidName_UpdatesName()
    {
        var st = new ServiceType("Oil Change", 100m);
        st.UpdateName("Tire Rotation");
        Assert.Equal("Tire Rotation", st.Name);
    }

    [Fact]
    public void ServiceType_UpdateName_WithEmptyName_ThrowsDomainException()
    {
        var st = new ServiceType("Oil Change", 100m);
        var ex = Assert.Throws<DomainException>(() => st.UpdateName(""));
        Assert.Equal("Service type name cannot be empty.", ex.Message);
    }

    // -----------------------------------------------------------------------
    // UpdateDescription
    // -----------------------------------------------------------------------

    [Fact]
    public void ServiceType_UpdateDescription_WithValue_SetsDescription()
    {
        var st = new ServiceType("Oil Change", 100m);
        st.UpdateDescription("New description");
        Assert.Equal("New description", st.Description);
    }

    [Fact]
    public void ServiceType_UpdateDescription_WithNull_SetsDescriptionNull()
    {
        var st = new ServiceType("Oil Change", 100m, "Initial description");
        st.UpdateDescription(null);
        Assert.Null(st.Description);
    }
}
