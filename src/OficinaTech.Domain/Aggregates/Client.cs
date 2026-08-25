using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Domain.Aggregates;

public class Client : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public TaxId TaxId { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }

    public Client(string name, TaxId taxId, string email, string phone, string address)
        : base(Guid.NewGuid())
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Client name cannot be empty.");
        TaxId = taxId ?? throw new DomainException("TaxId is required.");
        Email = IsPlausibleEmail(email)
            ? email
            : throw new DomainException($"'{email}' is not a valid email address.");
        Phone = phone ?? string.Empty;
        Address = !string.IsNullOrWhiteSpace(address)
            ? address
            : throw new DomainException("Address cannot be empty.");
    }

    private Client() : base() { }

    public void UpdateName(string name)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Client name cannot be empty.");
    }

    public void UpdateContactInfo(string email, string phone, string address)
    {
        Email = IsPlausibleEmail(email)
            ? email
            : throw new DomainException($"'{email}' is not a valid email address.");
        Phone = phone ?? string.Empty;
        Address = !string.IsNullOrWhiteSpace(address)
            ? address
            : throw new DomainException("Address cannot be empty.");
    }

    /// <summary>
    /// Lightweight structural email check: must have an '@' not at position 0,
    /// and at least one '.' after the '@' with at least one char before the final dot.
    /// </summary>
    private static bool IsPlausibleEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var at = email.IndexOf('@');
        return at > 0 && at < email.Length - 1 && email.IndexOf('.', at) > at + 1;
    }
}
