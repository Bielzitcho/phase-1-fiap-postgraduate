using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Presentation.Middleware;

internal sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Check ConcurrencyDomainException BEFORE the generic DomainException branch so that
        // concurrency conflicts always return 409, not 400, regardless of which endpoint
        // triggers them (eliminates the need for per-controller try/catch on every endpoint).
        if (exception is ConcurrencyDomainException concurrencyEx)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = concurrencyEx.Message
            }, cancellationToken);
            return true;
        }

        if (exception is not DomainException domainEx)
            return false;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = domainEx.Message
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
