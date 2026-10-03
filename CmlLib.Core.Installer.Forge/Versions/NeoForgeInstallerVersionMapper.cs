using CmlLib.Core.Installer.Forge.Installers;

namespace CmlLib.Core.Installer.Forge.Versions;

/// <summary>
/// Converts NeoForge version data to the shared installer input.
/// </summary>
public class NeoForgeInstallerVersionMapper
{
    public ForgeV12VersionArtifact CreateVersionArtifact(NeoForgeVersion version)
    {
        var minecraftVersion = NeoForgeVersionMapper.NormalizeMinecraftVersion(version.MinecraftVersionName);
        var loaderVersion = version.NeoForgeVersionName;
        return new ForgeV12VersionArtifact(
            MinecraftVersion: minecraftVersion,
            LoaderVersion: loaderVersion,
            VersionName: $"neoforge-{loaderVersion}",
            InstallerUrl: $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{loaderVersion}/neoforge-{loaderVersion}-installer.jar",
            EmbeddedVersionJar: null);
    }

    public ForgeV12Installer CreateInstaller(NeoForgeVersion version) => new(CreateVersionArtifact(version));
}
