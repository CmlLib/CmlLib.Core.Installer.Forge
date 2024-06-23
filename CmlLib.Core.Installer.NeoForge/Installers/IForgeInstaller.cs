using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;

namespace CmlLib.Core.Installer.Forge;

public interface IForgeInstaller
{
    string VersionName { get; }
    NeoForgeVersion NeoForgeVersion { get; }
    Task Install(MinecraftPath path, IGameInstaller installer, NeoForgeInstallOptions options);
}