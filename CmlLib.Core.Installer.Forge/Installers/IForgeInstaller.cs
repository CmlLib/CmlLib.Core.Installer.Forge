using CmlLib.Core.Installers;

namespace CmlLib.Core.Installer.Forge;

public interface IForgeInstaller
{
    string VersionName { get; }
    Task Install(MinecraftPath path, IGameInstaller installer, ForgeInstallOptions options);
}
