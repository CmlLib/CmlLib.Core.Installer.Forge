using CmlLib.Core.Installer.Forge.Installers;
using CmlLib.Core.Installer.Forge.Versions;

namespace CmlLib.Core.Installer.Forge.Tests;

public class ForgeInstallerVersionMapperTest
{
    [Theory]
    [InlineData(typeof(FNewest), "1.12.2", "14.23.5.2859", "1.12.2-forge-14.23.5.2859")]
    [InlineData(typeof(FNewest), "1.14.2", "26.0.63",      "1.14.2-forge-26.0.63")]
    [InlineData(typeof(FNewest), "1.14.4", "28.2.26",      "1.14.4-forge-28.2.26")]
    [InlineData(typeof(FNewest), "1.15.2", "31.2.57",      "1.15.2-forge-31.2.57")]
    [InlineData(typeof(FNewest), "1.16.5", "36.2.34",      "1.16.5-forge-36.2.34")]
    [InlineData(typeof(FNewest), "1.17.1", "37.1.1",       "1.17.1-forge-37.1.1")]
    [InlineData(typeof(FNewest), "1.18.1", "39.1.0",       "1.18.1-forge-39.1.0")]
    [InlineData(typeof(FNewest), "1.18.2", "40.2.0",       "1.18.2-forge-40.2.0")]
    [InlineData(typeof(FNewest), "1.19",   "41.1.0",       "1.19-forge-41.1.0")]
    [InlineData(typeof(FNewest), "1.19.4", "45.1.0",       "1.19.4-forge-45.1.0")]
    public void Test(Type installerType, string mcVersion, string forgeVersion, string versionName)
    {
        var mapper = new ForgeInstallerVersionMapper();
        var installer = mapper.CreateInstaller(new NeoForgeVersion(mcVersion, forgeVersion));
        Assert.IsType(installerType, installer);
        Assert.Equal(versionName, installer.VersionName);
    }
}