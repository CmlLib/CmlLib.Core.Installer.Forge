using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.Forge.Internal;

namespace CmlLib.Core.Installer.NeoForge;

/// <summary>
/// Discovers NeoForge versions and prepares vanilla Minecraft/Java before installation.
/// The NeoForge mapper creates ForgeV12Installer for the shared profile engine.
/// </summary>
public class NeoForgeInstaller
{
    private readonly NeoForgeVersionLoader _versionLoader;
    private readonly NeoForgeInstallerVersionMapper _installerMapper;
    private readonly InstallerRunner _runner;

    public NeoForgeInstaller(MinecraftLauncher launcher) : this(launcher, HttpUtil.DefaultClient.Value)
    {
    }

    public NeoForgeInstaller(MinecraftLauncher launcher, HttpClient httpClient)
    {
        _versionLoader = new NeoForgeVersionLoader(httpClient);
        _installerMapper = new NeoForgeInstallerVersionMapper();
        _runner = new InstallerRunner(launcher);
    }

    public Task<IReadOnlyList<NeoForgeVersion>> GetNeoForgeVersions() =>
        _versionLoader.GetNeoForgeVersions();

    public Task<IReadOnlyList<NeoForgeVersion>> GetNeoForgeVersions(string minecraftVersion) =>
        _versionLoader.GetNeoForgeVersions(minecraftVersion);

    public Task<string> Install(string minecraftVersion) => Install(minecraftVersion, new ForgeInstallOptions());

    public async Task<string> Install(string minecraftVersion, ForgeInstallOptions options)
    {
        var versions = await GetNeoForgeVersions(minecraftVersion);
        var latest = versions.FirstOrDefault() ??
            throw new InvalidOperationException($"Cannot find any NeoForge version for Minecraft {minecraftVersion}");
        return await Install(latest, options);
    }

    public Task<string> Install(string minecraftVersion, string neoForgeVersion) =>
        Install(minecraftVersion, neoForgeVersion, new ForgeInstallOptions());

    public async Task<string> Install(string minecraftVersion, string neoForgeVersion, ForgeInstallOptions options)
    {
        var versions = await GetNeoForgeVersions(minecraftVersion);
        var selected = versions.FirstOrDefault(v => v.NeoForgeVersionName == neoForgeVersion) ??
            throw new InvalidOperationException("Cannot find NeoForge version name " + neoForgeVersion);
        return await Install(selected, options);
    }

    public Task<string> Install(NeoForgeVersion neoForgeVersion, ForgeInstallOptions options)
    {
        var installer = _installerMapper.CreateInstaller(neoForgeVersion);
        return _runner.Install(installer.VersionArtifact.MinecraftVersion, installer, options);
    }
}
