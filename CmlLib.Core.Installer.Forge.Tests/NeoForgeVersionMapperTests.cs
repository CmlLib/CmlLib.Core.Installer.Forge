using CmlLib.Core.Installer.Forge.Versions;

namespace CmlLib.Core.Installer.Forge.Tests;

public class NeoForgeVersionMapperTests
{
    [Theory]
    [InlineData("20.2.88", "1.20.2")]
    [InlineData("20.6.119", "1.20.6")]
    [InlineData("21.0.167", "1.21")]
    [InlineData("21.11.10-beta", "1.21.11")]
    [InlineData("26.1.0.19-beta", "26.1")]
    [InlineData("26.1.2.114", "26.1.2")]
    [InlineData("27.2.1.3-preview", "27.2.1")]
    [InlineData("21.1.100-rc.1+build.2", "1.21.1")]
    [InlineData("26.1.0.0-alpha.1+snapshot-1", "26.1")]
    [InlineData("neoforge-21.1.100", "1.21.1")]
    [InlineData("neoforge-26.1.2.114-preview", "26.1.2")]
    [InlineData("0.25w14craftmine.5-beta", "25w14craftmine")]
    [InlineData("neoforge-0.25w14craftmine.5-beta", "25w14craftmine")]
    public void MapsNumericAndPrefixedReleaseNames(string name, string expected)
    {
        Assert.Equal(expected, NeoForgeVersionMapper.MapMinecraftVersion(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("custom-release")]
    [InlineData("neoforge-")]
    [InlineData("22.1.100")]
    [InlineData("21.1")]
    [InlineData("26.1.2")]
    [InlineData("21..100")]
    [InlineData("21.1.")]
    [InlineData("26.1..114")]
    [InlineData("99999999999999999999.1.2")]
    [InlineData("21.1.99999999999999999999")]
    [InlineData("0.25w14craftmine.invalid")]
    public void UnrecognizedNamesThrowWithOriginalReleaseName(string name)
    {
        var error = Assert.Throws<FormatException>(() => NeoForgeVersionMapper.MapMinecraftVersion(name));
        Assert.Contains($"'{name}'", error.Message);
    }

    [Theory]
    [InlineData("1.21.0", "1.21")]
    [InlineData("26.1.0", "26.1")]
    [InlineData("1.21.11", "1.21.11")]
    [InlineData("26.1.2", "26.1.2")]
    [InlineData("26.1", "26.1")]
    [InlineData("1.21.0.1", "1.21.0.1")]
    public void NormalizesOnlyTrailingZeroOfThreePartMinecraftVersions(string name, string expected)
    {
        Assert.Equal(expected, NeoForgeVersionMapper.NormalizeMinecraftVersion(name));
    }

    [Fact]
    public void MapsVersionWithoutChangingCatalogReleaseName()
    {
        const string name = "neoforge-26.1.2.114-preview+build.2";
        Assert.Equal(new NeoForgeVersion("26.1.2", name), NeoForgeVersionMapper.MapVersion(name));
    }
}
