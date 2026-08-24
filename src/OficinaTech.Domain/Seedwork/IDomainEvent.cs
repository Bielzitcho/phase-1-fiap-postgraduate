namespace OficinaTech.Domain.Seedwork;

// Marker interface — concrete events added in Phase 4
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
