using OficinaTech.Domain.Enums;
using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.ValueObjects;

public class TaxIdTests
{
    [Fact]
    public void ValidCpf_ShouldBeAccepted_AndNormalizeToDigitsOnly()
    {
        var taxId = new TaxId("123.456.789-09");
        Assert.Equal("12345678909", taxId.Value);
    }

    [Fact]
    public void ValidCpf_ShouldHaveCpfType()
    {
        var taxId = new TaxId("123.456.789-09");
        Assert.Equal(TaxIdType.Cpf, taxId.Type);
    }

    [Fact]
    public void AllSameDigitCpf_111_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new TaxId("111.111.111-11"));
    }

    [Fact]
    public void AllSameDigitCpf_000_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new TaxId("000.000.000-00"));
    }

    [Fact]
    public void WrongCheckDigitCpf_ShouldThrowDomainException()
    {
        // "123.456.789-00" — last two digits are wrong (should be 09)
        Assert.Throws<DomainException>(() => new TaxId("123.456.789-00"));
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("000.000.000-00")]
    [InlineData("123.456.789-00")]
    public void AllSameDigitOrInvalidCheckDigit_ShouldThrow(string input)
    {
        Assert.Throws<DomainException>(() => new TaxId(input));
    }

    [Fact]
    public void ValidCnpj_ShouldBeAccepted()
    {
        // 11.222.333/0001-81 is a valid CNPJ
        var taxId = new TaxId("11.222.333/0001-81");
        Assert.Equal(TaxIdType.Cnpj, taxId.Type);
    }

    [Fact]
    public void ValidCnpj_ShouldNormalizeToDigitsOnly()
    {
        var taxId = new TaxId("11.222.333/0001-81");
        Assert.Equal("11222333000181", taxId.Value);
    }

    [Fact]
    public void InvalidCnpj_WrongCheckDigit_ShouldThrowDomainException()
    {
        // 11.222.333/0001-00 — wrong check digits
        Assert.Throws<DomainException>(() => new TaxId("11.222.333/0001-00"));
    }

    [Fact]
    public void TwelveDigitString_ShouldThrowDomainException()
    {
        // 12 digits — neither CPF (11) nor CNPJ (14)
        Assert.Throws<DomainException>(() => new TaxId("123456789012"));
    }
}
