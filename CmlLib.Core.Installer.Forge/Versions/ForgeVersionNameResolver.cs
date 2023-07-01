namespace CmlLib.Core.Installer.Forge.Versions;

public class ForgeVersionNameResolver
{

    public string ResolveVersionName(string mcVersion, string forgeVersion)
    {
        var midVersionStr = mcVersion.Split('.')[1];
        if (string.IsNullOrEmpty(midVersionStr))
            throw new FormatException();

        switch (mcVersion)
        {
            case "1.7.10":
                return $"{mcVersion}-Forge{forgeVersion}-{mcVersion}";
            case "1.8.9":
            case "1.9.4":
                return $"{mcVersion}-forge{mcVersion}-{forgeVersion}-{mcVersion}";
            case "1.10":
                return $"1.10-forge1.10-{forgeVersion}-1.10.0";
            case "1.12.2":
                return $"{mcVersion}-forge-{forgeVersion}";
        }

        var midVersion = int.Parse(midVersionStr);
        return midVersion switch
        {
            <= 7 => $"{mcVersion}-Forge{forgeVersion}",
            <= 12 => $"{mcVersion}-forge{mcVersion}-{forgeVersion}",
            _ => $"{mcVersion}-forge-{forgeVersion}"
        };
    }
}
