using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Seedwork;
using Xunit;

namespace OficinaTech.Tests.Aggregates;

public class ServiceOrderTests
{
    [Fact]
    public void NewServiceOrder_ShouldHaveStatusRecebida()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(ServiceOrderStatus.Recebida, order.Status);
    }

    [Fact]
    public void AllSixForwardTransitions_ShouldSucceed()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(ServiceOrderStatus.Recebida, order.Status);

        order.StartDiagnosis();
        Assert.Equal(ServiceOrderStatus.EmDiagnostico, order.Status);

        order.SendForApproval();
        Assert.Equal(ServiceOrderStatus.AguardandoAprovacao, order.Status);

        order.Approve();
        Assert.Equal(ServiceOrderStatus.EmExecucao, order.Status);

        order.Finalize();
        Assert.Equal(ServiceOrderStatus.Finalizada, order.Status);

        order.MarkDelivered();
        Assert.Equal(ServiceOrderStatus.Entregue, order.Status);
    }

    [Fact]
    public void SendForApproval_OnFreshOrder_ShouldThrowDomainException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<DomainException>((Action)(() => order.SendForApproval()));
    }

    [Fact]
    public void Approve_WhileEmDiagnostico_ShouldThrowDomainException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.StartDiagnosis();  // now EmDiagnostico
        Assert.Throws<DomainException>((Action)(() => order.Approve()));
    }

    [Fact]
    public void Finalize_WhileAguardandoAprovacao_ShouldThrowDomainException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.StartDiagnosis();
        order.SendForApproval();  // now AguardandoAprovacao
        Assert.Throws<DomainException>((Action)(() => order.Finalize()));
    }

    [Fact]
    public void MarkDelivered_WhileEmExecucao_ShouldThrowDomainException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.StartDiagnosis();
        order.SendForApproval();
        order.Approve();  // now EmExecucao
        Assert.Throws<DomainException>((Action)(() => order.MarkDelivered()));
    }

    [Fact]
    public void StartDiagnosis_Twice_ShouldThrowDomainExceptionOnSecondCall()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.StartDiagnosis();  // now EmDiagnostico
        Assert.Throws<DomainException>((Action)(() => order.StartDiagnosis()));
    }

    [Fact]
    public void StartExecution_WhileEmExecucao_ShouldSucceedAndLeaveStatusUnchanged()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.StartDiagnosis();
        order.SendForApproval();
        order.Approve();  // now EmExecucao

        order.StartExecution();  // idempotent guard — no status change
        Assert.Equal(ServiceOrderStatus.EmExecucao, order.Status);
    }

    [Fact]
    public void StartExecution_WhileRecebida_ShouldThrowDomainException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<DomainException>((Action)(() => order.StartExecution()));
    }

    [Fact]
    public void AddService_AndAddPart_WhileRecebida_ShouldSucceedAndComputeTotalAmount()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.AddService(Guid.NewGuid(), "Oil Change", 50.00m);
        order.AddPart(Guid.NewGuid(), "Oil Filter", 20.00m, 2);

        // TotalAmount = 50 + (20 * 2) = 90
        Assert.Equal(90.00m, order.TotalAmount);
    }

    [Fact]
    public void AddService_AfterApprove_ShouldThrowDomainException()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid());
        order.StartDiagnosis();
        order.SendForApproval();
        order.Approve();  // now EmExecucao

        Assert.Throws<DomainException>((Action)(() =>
            order.AddService(Guid.NewGuid(), "Oil Change", 50.00m)));
    }

    [Fact]
    public void Constructor_WithEmptyClientId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>((Action)(() =>
            new ServiceOrder(Guid.Empty, Guid.NewGuid())));
    }
}
