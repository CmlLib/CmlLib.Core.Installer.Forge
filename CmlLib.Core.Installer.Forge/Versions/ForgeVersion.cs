namespace CmlLib.Core.Installer.Forge.Versions;

public class ForgeVersion
{
    public string? MinecraftVersionName { get; set; }
    public string? ForgeVersionName { get; set; }
    public string? Time { get; set; }
    public IEnumerable<ForgeVersionFile>? Files { get; set; }
    public bool IsLatestVersion { get; set; }
    public bool IsRecommendedVersion { get; set; }

    public ForgeVersionFile? GetInstallerFile()
    {
        if (Files == null)
            return null;
        return Files.FirstOrDefault(file => file.Type?.ToLowerInvariant() == "installer");
    }
}
