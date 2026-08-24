namespace OficinaTech.Domain.Seedwork;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }

    protected Entity(TId id) => Id = id;

    // EF Core requires parameterless constructor for owned types / proxies
    protected Entity() { }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() =>
        (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !(left == right);
}
