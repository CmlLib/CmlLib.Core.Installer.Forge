namespace CmlLib.Core.Installer.Forge.Installers;

// Format-independent input for modern Forge/NeoForge installer profiles.
public sealed record ForgeV12VersionArtifact(
    string MinecraftVersion,
    string LoaderVersion,
    string VersionName,
    string? InstallerUrl,
    string? EmbeddedVersionJar
);