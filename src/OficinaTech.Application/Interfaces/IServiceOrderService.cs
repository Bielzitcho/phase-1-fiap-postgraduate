using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IServiceOrderService
{
    Task<Result<ServiceOrderResponse>> CreateAsync(CreateServiceOrderRequest req, CancellationToken ct = default);
    Task<Result<ServiceOrderResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    // D-12: admin list with optional status/clientId filters and pagination (OS-10)
    Task<Result<PagedResult<ServiceOrderSummaryResponse>>> GetAllAsync(
        ServiceOrderStatus? status, Guid? clientId, int page, int pageSize, CancellationToken ct = default);

    // D-13: public status query keyed by taxId (OS-12)
    Task<Result<IReadOnlyList<PublicServiceOrderSummary>>> GetByTaxIdAsync(
        string taxId, CancellationToken ct = default);

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
