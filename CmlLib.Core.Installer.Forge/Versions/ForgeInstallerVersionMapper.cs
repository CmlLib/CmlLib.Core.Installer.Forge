using CmlLib.Core.Installer.Forge.Installers;

namespace CmlLib.Core.Installer.Forge.Versions;

public class ForgeInstallerVersionMapper : IForgeInstallerVersionMapper
{
    public IForgeInstaller CreateOldInstaller(string versionName, ForgeVersion version) =>
        new FLegacy(versionName, version);

    public IForgeInstaller Create1_7_10Installer(string versionName, ForgeVersion version) => 
        new FLegacy(versionName, version);

    public IForgeInstaller Create1_12_2Installer(string versionName, ForgeVersion version) =>
        new FNewest(versionName, version);

    public IForgeInstaller CreateInstaller(ForgeVersion version)
    {
        var m = version.MinecraftVersionName;
        var f = version.ForgeVersionName;
        var versionSplit = m.Split('.');
        var major = int.Parse(versionSplit[0]);
        var minor = int.Parse(versionSplit[1]);

        return (major, minor) switch
        {
            (1, <= 6) => resolveOld(version), // oldest version ~ 1.6.*
            (1, <= 12) => resolve1_7_2_1_12_2(version), // 1.7.2 ~ 1.12.2
            _ => Create1_12_2Installer($"{m}-forge-{f}", version) // 1.13.* ~ latest version
        };
    }

    private IForgeInstaller resolveOld(ForgeVersion version)
    {
        var m = version.MinecraftVersionName;
        var f = version.ForgeVersionName;
        return (m, f) switch
        {
            ("1.6.1", _) => CreateOldInstaller($"Forge{f}", version),
            _ => CreateOldInstaller($"{m}-Forge{f}", version)
        };
    }

    // 1.7.2 ~ 1.12.2 is just chaos
    private IForgeInstaller resolve1_7_2_1_12_2(ForgeVersion version)
    {
        var m = version.MinecraftVersionName;
        var f = version.ForgeVersionName;
        return (m, f) switch
        {
            ("1.7.2", _) => CreateOldInstaller($"1.7.2-Forge{f}-mc172", version),
            ("1.7.10-pre4", _) => Create1_7_10Installer($"1.7.10-pre4-Forge{f}-prerelease", version),
            ("1.7.10", _) => Create1_7_10Installer($"1.7.10-Forge{f}-1.7.10", version),

            ("1.8", _) => Create1_7_10Installer(mf(m, f), version),
            ("1.8.8", _) => Create1_7_10Installer(mf(m, f), version),
            ("1.8.9", _) => Create1_7_10Installer(mfm(m, f), version),

            ("1.9", "12.16.1.1938") => Create1_7_10Installer(mfm0(m, f), version),
            ("1.9", _) => Create1_7_10Installer(mf(m, f), version),
            ("1.9.4", _) => Create1_7_10Installer(mfm(m, f), version),

            ("1.10", _) => Create1_7_10Installer(mfm0(m, f), version),
            ("1.10.2", _) or
            ("1.11", _) or
            ("1.11.2", _) or
            ("1.12", _) or
            ("1.12.1", _) => Create1_7_10Installer(mf(m, f), version),
            ("1.12.2", _) => Create1_12_2Installer($"1.12.2-forge-{f}", version),

            _ => Create1_12_2Installer(mf(m, f), version)
        };
    }

    private string mf(string m, string f)
    {
        return $"{m}-forge{m}-{f}";
    }

    private string mfm(string m, string f)
    {
        return $"{m}-forge{m}-{f}-{m}";
    }

    private string mfm0(string m, string f)
    {
        return $"{m}-forge{m}-{f}-{m}.0";
    }
}
