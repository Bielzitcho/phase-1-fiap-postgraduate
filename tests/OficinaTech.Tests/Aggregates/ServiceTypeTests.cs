using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Seedwork;
using Xunit;

namespace OficinaTech.Tests.Aggregates;

public class ServiceTypeTests
{
    [Fact]
    public void NewServiceType_AverageExecutionTime_ShouldBeZero()
    {
        var serviceType = new ServiceType("Oil Change", 100m);
        Assert.Equal(TimeSpan.Zero, serviceType.AverageExecutionTime);
    }

    [Fact]
    public void ServiceType_AfterTwoExecutions_ShouldComputeCorrectAverage()
    {
        var serviceType = new ServiceType("Oil Change", 100m);
        serviceType.RecordExecution(TimeSpan.FromMinutes(60));
        serviceType.RecordExecution(TimeSpan.FromMinutes(120));
        Assert.Equal(TimeSpan.FromMinutes(90), serviceType.AverageExecutionTime);
    }

    [Fact]
    public void ServiceType_RecordExecution_WithNegativeDuration_ShouldThrowDomainException()
    {
        var serviceType = new ServiceType("Oil Change", 100m);
        Assert.Throws<DomainException>(() =>
            serviceType.RecordExecution(TimeSpan.FromMinutes(-5)));
    }

    [Fact]
    public void ServiceType_WithEmptyName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new ServiceType("", 100m));
    }

    [Fact]
    public void ServiceType_WithNonPositiveBasePrice_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new ServiceType("Oil Change", 0m));
    }
}
