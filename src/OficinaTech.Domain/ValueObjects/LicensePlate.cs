using System.Text.RegularExpressions;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.ValueObjects;

public sealed class LicensePlate : ValueObject
{
    // Covers both formats on the 7-character normalized string (hyphen stripped):
    // Old: ABC1234 → digit 5 is a digit (positions 3-6 are all digits)
    // Mercosul: ABC1D23 → digit 5 is a letter
    private static readonly Regex _regex = new(
        @"^[A-Z]{3}[0-9][0-9A-Z][0-9]{2}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }  // normalized: uppercase, no hyphen

    public LicensePlate(string value)
    {
        var normalized = value?.Replace("-", "").Trim().ToUpperInvariant() ?? "";
        if (!_regex.IsMatch(normalized))
            throw new DomainException(
                $"'{value}' is not a valid license plate. Expected old format (ABC-1234) or Mercosul (ABC1D23).");
        Value = normalized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
