using CmlLib.Core.Installer.Forge.Versions;

namespace CmlLib.Core.Installer.Forge.Tests;

public class ForgeVersionNameResolverTest
{
    [Theory]
    [InlineData("1.5.2",  "7.8.1.738",   "1.5.2-Forge7.8.1.738")]
    [InlineData("1.6.4",  "9.11.1.1345", "1.6.4-Forge9.11.1.1345")]
    [InlineData("1.7.2",  "10.12.2.1161", "1.7.2-Forge10.12.2.1161-mc172")]
    [InlineData("1.7.10-pre4", "10.12.2.1149", "1.7.10-pre4-Forge10.12.2.1149-prerelease")]
    [InlineData("1.7.10", "10.13.4.1614", "1.7.10-Forge10.13.4.1614-1.7.10")]
    [InlineData("1.8",    "11.14.4.1563", "1.8-forge1.8-11.14.4.1563")]
    [InlineData("1.8.8",  "11.15.0.1655", "1.8.8-forge1.8.8-11.15.0.1655")]
    [InlineData("1.8.9",  "11.15.1.2318", "1.8.9-forge1.8.9-11.15.1.2318-1.8.9")]
    [InlineData("1.9",    "12.16.1.1887", "1.9-forge1.9-12.16.1.1887")]
    [InlineData("1.9",    "12.16.1.1934", "1.9-forge1.9-12.16.1.1934")]
    [InlineData("1.9",    "12.16.1.1938", "1.9-forge1.9-12.16.1.1938-1.9.0")]
    [InlineData("1.9.4",  "12.17.0.2317", "1.9.4-forge1.9.4-12.17.0.2317-1.9.4")]
    [InlineData("1.10",   "12.18.0.2000", "1.10-forge1.10-12.18.0.2000-1.10.0")]
    [InlineData("1.10.2", "12.18.3.2511", "1.10.2-forge1.10.2-12.18.3.2511")]
    [InlineData("1.11",   "13.19.1.2189", "1.11-forge1.11-13.19.1.2189")]
    [InlineData("1.11.2", "13.20.1.2588", "1.11.2-forge1.11.2-13.20.1.2588")]
    [InlineData("1.12",   "14.21.1.2387", "1.12-forge1.12-14.21.1.2387")]
    [InlineData("1.12.1", "14.22.1.2478", "1.12.1-forge1.12.1-14.22.1.2478")]
    [InlineData("1.12.2", "14.23.5.2859", "1.12.2-forge-14.23.5.2859")]
    [InlineData("1.14.2", "26.0.63",      "1.14.2-forge-26.0.63")]
    [InlineData("1.14.4", "28.2.26",      "1.14.4-forge-28.2.26")]
    [InlineData("1.15.2", "31.2.57",      "1.15.2-forge-31.2.57")]
    [InlineData("1.16.5", "36.2.34",      "1.16.5-forge-36.2.34")]
    [InlineData("1.17.1", "37.1.1",       "1.17.1-forge-37.1.1")]
    [InlineData("1.18.1", "39.1.0",       "1.18.1-forge-39.1.0")]
    [InlineData("1.18.2", "40.2.0",       "1.18.2-forge-40.2.0")]
    [InlineData("1.19",   "41.1.0",       "1.19-forge-41.1.0")]
    [InlineData("1.19.4", "45.1.0",       "1.19.4-forge-45.1.0")]
    public void Test(string mcVersion, string forgeVersion, string versionName)
    {
        var resolver = new ForgeVersionNameResolver();
        var result = resolver.ResolveVersionName(mcVersion, forgeVersion);
        Assert.Equal(versionName, result);
    }
}