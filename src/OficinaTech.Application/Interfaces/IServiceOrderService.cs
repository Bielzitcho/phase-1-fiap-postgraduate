using OficinaTech.Application.DTOs;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Application.Interfaces;

public interface IServiceOrderService
{
    Task<Result<ServiceOrderResponse>> CreateAsync(CreateServiceOrderRequest req, CancellationToken ct = default);
    Task<Result<ServiceOrderResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
