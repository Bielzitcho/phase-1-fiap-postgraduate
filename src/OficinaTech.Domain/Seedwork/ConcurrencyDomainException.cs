namespace OficinaTech.Domain.Seedwork;

/// <summary>
/// Thrown when an optimistic concurrency conflict is detected (e.g., a concurrent write
/// modified the same record). Maps to HTTP 409 Conflict at the presentation layer.
/// Extends DomainException to pass through the global DomainExceptionHandler; controllers
/// that need 409 (vs 400) catch this subtype specifically.
/// Domain layer has zero dependency on EF Core — the throw originates in Infrastructure
/// (EfUnitOfWork) but the type lives in Domain to allow the Presentation layer to catch it
/// without referencing Infrastructure.
/// </summary>
public sealed class ConcurrencyDomainException : DomainException
{
    public ConcurrencyDomainException(string message)
        : base(message) { }

    public ConcurrencyDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
