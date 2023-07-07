using CmlLib.Core.Installer.Forge.Versions;

namespace CmlLib.Core.Installer.Forge.Installers;

public class FOldest : ForgeInstaller
{
    public FOldest(string versionName, ForgeVersion forgeVersion) : base(versionName, forgeVersion)
    {
    }

    protected override Task Install(string installerDir)
    {
        throw new UnsupportedForgeVersionException(VersionName);
    }
}
