namespace CmlLib.Core.Installer.Forge.Versions;

public class NeoForgeVersion
{
    public NeoForgeVersion(string minecraftVersion, string version)
    {
        MinecraftVersion = minecraftVersion;
        VersionName = version;
    }

    public string NeoForgeUrl =>
        $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{VersionName}/neoforge-{VersionName}-installer.jar";
    public string MinecraftVersion { get; set; }
    public string VersionName { get; }
    public IEnumerable<ForgeVersionFile>? Files { get; set; }

    public ForgeVersionFile? GetInstallerFile()
    {
        return new ForgeVersionFile
        {
            DirectUrl = NeoForgeUrl
        };
    }
}
