using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IServiceOrderService
{
    Task<Result<ServiceOrderResponse>> CreateAsync(CreateServiceOrderRequest req, CancellationToken ct = default);
    Task<Result<ServiceOrderResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Status lifecycle transitions (admin-only)
    Task<Result<ServiceOrderSummaryResponse>> StartDiagnosisAsync(Guid id, CancellationToken ct = default);
    Task<Result<ServiceOrderSummaryResponse>> SendForApprovalAsync(Guid id, CancellationToken ct = default);
    Task<Result<ServiceOrderSummaryResponse>> FinalizeAsync(Guid id, CancellationToken ct = default);
    Task<Result<ServiceOrderSummaryResponse>> MarkDeliveredAsync(Guid id, CancellationToken ct = default);

    // Public approval endpoint (AllowAnonymous, D-05)
    Task<Result<ServiceOrderSummaryResponse>> ApproveAsync(Guid id, ApproveServiceOrderRequest req, CancellationToken ct = default);

    // Incremental item addition (admin-only, D-04)
    Task<Result<ServiceOrderResponse>> AddServiceAsync(Guid id, AddServiceRequest req, CancellationToken ct = default);
    Task<Result<ServiceOrderResponse>> AddPartAsync(Guid id, AddPartRequest req, CancellationToken ct = default);
}
