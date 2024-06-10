using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;

namespace CmlLib.Core.Installer.Forge.Installers;

public class FOldest : IForgeInstaller
{
    public FOldest(string versionName, ForgeVersion forgeVersion)
    {
        VersionName = versionName;
        ForgeVersion = forgeVersion;
    }

    public string VersionName { get; }

    public ForgeVersion ForgeVersion { get; }

    public Task Install(MinecraftPath path, IGameInstaller installer, ForgeInstallOptions options)
    {
        throw new UnsupportedForgeVersionException(VersionName);
    }
}
