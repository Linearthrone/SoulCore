using SoulCore.Core.Persona;

namespace SoulCore.Protocol.Tests;

public class HumanAddressCorrectionTests
{
    [Theory]
    [InlineData("please call me Kayleigh", "Kayleigh")]
    [InlineData("Call me Alex.", "Alex")]
    [InlineData("my name is Sam", "Sam")]
    [InlineData("address me as Jordan", "Jordan")]
    [InlineData("I go by Riley", "Riley")]
    [InlineData("stop calling me Kurt, call me Kayleigh", "Kayleigh")]
    [InlineData("don't call me OldName call me NewName", "NewName")]
    public void TryExtractNewAddress_FindsStandingName(string text, string expected)
    {
        Assert.Equal(expected, HumanAddressCorrection.TryExtractNewAddress(text));
    }

    [Theory]
    [InlineData("stop calling me Kurt")]
    [InlineData("how are you")]
    [InlineData("I'm watching the video")]
    [InlineData("")]
    [InlineData(null)]
    public void TryExtractNewAddress_IgnoresNonCorrections(string? text)
    {
        Assert.Null(HumanAddressCorrection.TryExtractNewAddress(text));
    }

    [Fact]
    public void IsChange_IsCaseInsensitive()
    {
        Assert.False(HumanAddressCorrection.IsChange("Kayleigh", "kayleigh"));
        Assert.True(HumanAddressCorrection.IsChange("Friend", "Kayleigh"));
    }
}
