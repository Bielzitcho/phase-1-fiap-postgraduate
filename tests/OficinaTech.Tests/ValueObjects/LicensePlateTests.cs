using OficinaTech.Domain.Seedwork;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.ValueObjects;

public class LicensePlateTests
{
    [Theory]
    [InlineData("ABC-1234", "ABC1234")]   // old format, hyphen stripped
    [InlineData("ABC1D23", "ABC1D23")]    // Mercosul
    [InlineData("abc-1234", "ABC1234")]   // lowercase normalized
    public void ValidPlate_ShouldBeAccepted(string input, string expectedNormalized)
    {
        var plate = new LicensePlate(input);
        Assert.Equal(expectedNormalized, plate.Value);
    }

    [Theory]
    [InlineData("ABC12345")]   // too many digits
    [InlineData("ABCD123")]    // 4 letters
    [InlineData("")]
    [InlineData(null)]
    public void InvalidPlate_ShouldThrowDomainException(string? input)
    {
        Assert.Throws<DomainException>(() => new LicensePlate(input!));
    }
}
