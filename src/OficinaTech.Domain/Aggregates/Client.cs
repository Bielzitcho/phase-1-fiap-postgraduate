using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Domain.Aggregates;

public class Client : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public TaxId TaxId { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }

    public Client(string name, TaxId taxId, string email, string phone)
        : base(Guid.NewGuid())
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new DomainException("Client name cannot be empty.");
        TaxId = taxId ?? throw new DomainException("TaxId is required.");
        Email = !string.IsNullOrWhiteSpace(email)
            ? email
            : throw new DomainException("Email cannot be empty.");
        Phone = phone ?? string.Empty;
    }

    private Client() : base() { }

    public void UpdateContactInfo(string email, string phone)
    {
        Email = !string.IsNullOrWhiteSpace(email)
            ? email
            : throw new DomainException("Email cannot be empty.");
        Phone = phone ?? string.Empty;
    }
}
