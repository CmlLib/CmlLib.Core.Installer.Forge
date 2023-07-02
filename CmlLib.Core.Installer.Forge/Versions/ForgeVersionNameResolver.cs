namespace CmlLib.Core.Installer.Forge.Versions;

public class ForgeVersionNameResolver : IForgeVersionNameResolver
{
    public string Resolve(string mcVersion, string forgeVersion)
    {
        var versionSplit = mcVersion.Split('.');
        var major = int.Parse(versionSplit[0]);
        var minor = int.Parse(versionSplit[1]);

        return (major, minor) switch
        {
            (1, <= 6) => $"{mcVersion}-Forge{forgeVersion}", // oldest version ~ 1.6.*
            (1, <= 12) => resolve172(mcVersion, forgeVersion), // 1.7.2 ~ 1.12.2
            _ => $"{mcVersion}-forge-{forgeVersion}" // 1.13.* ~ latest version
        };
    }

    // 1.7.2 ~ 1.12.2 is just chaos
    private string resolve172(string m, string f)
    {
        return (m, f) switch
        {
            ("1.7.2", _) => $"1.7.2-Forge{f}-mc172",
            ("1.7.10-pre4", _) => $"1.7.10-pre4-Forge{f}-prerelease",
            ("1.7.10", _) => $"1.7.10-Forge{f}-1.7.10",

            ("1.8", _) => mf(m, f),
            ("1.8.8", _) => mf(m, f),
            ("1.8.9", _) => mfm(m, f),

            ("1.9", "12.16.1.1938") => mfm0(m, f), 
            ("1.9", _) => mf(m, f),
            ("1.9.4", _) => mfm(m, f),

            ("1.10", _) => mfm0(m, f),
            ("1.10.2", _) or
            ("1.11", _) or
            ("1.11.2", _) or
            ("1.12", _) or
            ("1.12.1", _) => mf(m, f),
            ("1.12.2", _) => $"1.12.2-forge-{f}",

            _ => mf(m, f)
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
