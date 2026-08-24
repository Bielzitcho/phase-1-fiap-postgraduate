using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Seedwork;

namespace OficinaTech.Domain.ValueObjects;

public sealed class TaxId : ValueObject
{
    public TaxIdType Type { get; }
    public string Value { get; }  // digits only, e.g. "12345678909"

    public string FormattedValue => Type == TaxIdType.Cpf
        ? $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..11]}"
        : $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..14]}";

    public TaxId(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && IsValidCpf(digits))
        {
            Type = TaxIdType.Cpf;
            Value = digits;
        }
        else if (digits.Length == 14 && IsValidCnpj(digits))
        {
            Type = TaxIdType.Cnpj;
            Value = digits;
        }
        else
        {
            throw new DomainException(
                $"'{value}' is not a valid CPF or CNPJ. Ensure format and check digits are correct.");
        }
    }

    /// <summary>
    /// Validates an 11-digit CPF string using the Receita Federal check digit algorithm.
    /// Rejects all-same-digit inputs before running check digit math.
    /// </summary>
    private static bool IsValidCpf(string digits)
    {
        // Reject all-same-digit inputs (e.g., "00000000000", "11111111111")
        if (digits.Distinct().Count() == 1) return false;

        // First check digit: weights 10 down to 2 applied to digits[0..8]
        var sum1 = 0;
        for (var i = 0; i < 9; i++)
            sum1 += (digits[i] - '0') * (10 - i);

        var remainder1 = sum1 % 11;
        var checkDigit1 = remainder1 < 2 ? 0 : 11 - remainder1;

        if (checkDigit1 != (digits[9] - '0')) return false;

        // Second check digit: weights 11 down to 2 applied to digits[0..9]
        var sum2 = 0;
        for (var i = 0; i < 10; i++)
            sum2 += (digits[i] - '0') * (11 - i);

        var remainder2 = sum2 % 11;
        var checkDigit2 = remainder2 < 2 ? 0 : 11 - remainder2;

        return checkDigit2 == (digits[10] - '0');
    }

    /// <summary>
    /// Validates a 14-digit CNPJ string using the Receita Federal check digit algorithm.
    /// Rejects all-same-digit inputs before running check digit math.
    /// </summary>
    private static bool IsValidCnpj(string digits)
    {
        // Reject all-same-digit inputs (e.g., "00000000000000", "11111111111111")
        if (digits.Distinct().Count() == 1) return false;

        // First check digit: weights [5,4,3,2,9,8,7,6,5,4,3,2] applied to digits[0..11]
        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum1 = 0;
        for (var i = 0; i < 12; i++)
            sum1 += (digits[i] - '0') * weights1[i];

        var remainder1 = sum1 % 11;
        var checkDigit1 = remainder1 < 2 ? 0 : 11 - remainder1;

        if (checkDigit1 != (digits[12] - '0')) return false;

        // Second check digit: weights [6,5,4,3,2,9,8,7,6,5,4,3,2] applied to digits[0..12]
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum2 = 0;
        for (var i = 0; i < 13; i++)
            sum2 += (digits[i] - '0') * weights2[i];

        var remainder2 = sum2 % 11;
        var checkDigit2 = remainder2 < 2 ? 0 : 11 - remainder2;

        return checkDigit2 == (digits[13] - '0');
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }
}
