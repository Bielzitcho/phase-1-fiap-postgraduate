using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Presentation.Controllers;

[ApiController]
[Route("api/service-orders")]
[Authorize(Roles = "admin")]
public class ServiceOrdersController : ControllerBase
{
    private readonly IServiceOrderService _service;

    public ServiceOrdersController(IServiceOrderService service) => _service = service;

    // -----------------------------------------------------------------------
    // Query endpoints (D-12, D-13) — Plan 03
    // -----------------------------------------------------------------------

    // D-12: admin filtered paged list (OS-10)
    // Class-level [Authorize(Roles = "admin")] protects this endpoint.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ServiceOrderStatus? status,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(status, clientId, page, pageSize, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 500);
    }

    // D-13: public by-client status query (OS-12)
    // [AllowAnonymous] overrides class-level [Authorize(Roles = "admin")].
    // Route "by-client" is a literal segment — cannot be captured by the {id:guid} route constraint.
    [HttpGet("by-client")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByTaxId(
        [FromQuery] string taxId,
        CancellationToken ct = default)
    {
        var result = await _service.GetByTaxIdAsync(taxId, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    // -----------------------------------------------------------------------
    // CRUD endpoints (Plan 01)
    // -----------------------------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceOrderRequest req, CancellationToken ct)
    {
        var result = await _service.CreateAsync(req, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

        // D-02/OS-02: client or vehicle resolution failure → 404
        var statusCode = result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
            ? 404
            : 400;
        return Problem(detail: result.Error, statusCode: statusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error, statusCode: 404);
    }

    // -----------------------------------------------------------------------
    // Admin lifecycle transition endpoints (PATCH slugged-action, Plan 02)
    // Class-level [Authorize(Roles = "admin")] applies to all transitions.
    // Wrong-status DomainExceptions bubble to the global handler → 400.
    // -----------------------------------------------------------------------

    [HttpPatch("{id:guid}/start-diagnosis")]
    public async Task<IActionResult> StartDiagnosis(Guid id, CancellationToken ct)
    {
        var result = await _service.StartDiagnosisAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpPatch("{id:guid}/send-for-approval")]
    public async Task<IActionResult> SendForApproval(Guid id, CancellationToken ct)
    {
        var result = await _service.SendForApprovalAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpPatch("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct)
    {
        var result = await _service.FinalizeAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpPatch("{id:guid}/mark-delivered")]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken ct)
    {
        var result = await _service.MarkDeliveredAsync(id, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    // -----------------------------------------------------------------------
    // Incremental item addition (admin-only, D-04)
    // -----------------------------------------------------------------------

    [HttpPost("{id:guid}/services")]
    public async Task<IActionResult> AddService(
        Guid id, [FromBody] AddServiceRequest req, CancellationToken ct)
    {
        var result = await _service.AddServiceAsync(id, req, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode:
                result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? 404 : 400);
    }

    [HttpPost("{id:guid}/parts")]
    public async Task<IActionResult> AddPart(
        Guid id, [FromBody] AddPartRequest req, CancellationToken ct)
    {
        var result = await _service.AddPartAsync(id, req, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode:
                result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? 404 : 400);
    }

    // -----------------------------------------------------------------------
    // Public approval endpoint (AllowAnonymous, D-05)
    // - Mismatch taxId → 403
    // - Concurrent double-approval → 409 via ConcurrencyDomainException catch (D-09)
    // - Wrong-status → 400 via global DomainExceptionHandler
    // -----------------------------------------------------------------------

    [HttpPost("{id:guid}/approve")]
    [AllowAnonymous]
    public async Task<IActionResult> Approve(
        Guid id, [FromBody] ApproveServiceOrderRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _service.ApproveAsync(id, req, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            // Map failure errors to appropriate status codes
            if (result.Error?.Contains("does not match", StringComparison.OrdinalIgnoreCase) == true)
                return Problem(detail: result.Error, statusCode: 403);
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return Problem(detail: result.Error, statusCode: 404);

            return Problem(detail: result.Error, statusCode: 400);
        }
        catch (ConcurrencyDomainException ex)
        {
            // D-09: concurrent approval → 409 Conflict
            return Conflict(new { detail = ex.Message });
        }
    }
}
