using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.Forge.Internal;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge;

public class ForgeInstaller
{
    public static readonly string ForgeAdUrl =
        "https://adfoc.us/serve/sitelinks/?id=271228&url=https://maven.minecraftforge.net/";

    private readonly InstallerRunner _runner;
    private readonly IForgeInstallerVersionMapper _installerMapper;
    private readonly IForgeVersionLoader _versionLoader;

    public ForgeInstaller(MinecraftLauncher launcher) : this(launcher, HttpUtil.DefaultClient.Value)
    {

    }

    public ForgeInstaller(MinecraftLauncher launcher, HttpClient httpClient)
        : this(launcher, new ForgeVersionLoader(httpClient), new ForgeInstallerVersionMapper())
    {
    }

    public ForgeInstaller(
        MinecraftLauncher launcher,
        IForgeVersionLoader versionLoader,
        IForgeInstallerVersionMapper installerMapper)
    {
        _versionLoader = versionLoader;
        _installerMapper = installerMapper;
        _runner = new InstallerRunner(launcher);
    }

    public Task<string> Install(string mcVersion) =>
        Install(mcVersion, new ForgeInstallOptions());

    public async Task<string> Install(
        string mcVersion,
        ForgeInstallOptions options)
    {
        var versions = await _versionLoader.GetForgeVersions(mcVersion);
        var bestVersion =
            versions.FirstOrDefault(v => v.IsRecommendedVersion) ??
            versions.FirstOrDefault(v => v.IsLatestVersion) ??
            versions.FirstOrDefault() ??
            throw new InvalidOperationException("Cannot find any version");

        return await Install(bestVersion, options);
    }

    public Task<IEnumerable<ForgeVersion>> GetForgeVersions(string mcVersion)
    {
        return _versionLoader.GetForgeVersions(mcVersion);
    }

    public Task<string> Install(string mcVersion, string forgeVersion) =>
        Install(mcVersion, forgeVersion, new ForgeInstallOptions());

    public async Task<string> Install(
        string mcVersion,
        string forgeVersion,
        ForgeInstallOptions options)
    {
        var versions = await _versionLoader.GetForgeVersions(mcVersion);

        var foundVersion = versions.FirstOrDefault(v => v.ForgeVersionName == forgeVersion) ??
            throw new InvalidOperationException("Cannot find version name " + forgeVersion);
        return await Install(foundVersion, options);
    }

    public Task<string> Install(ForgeVersion forgeVersion, ForgeInstallOptions options)
    {
        var installer = _installerMapper.CreateInstaller(forgeVersion);
        return _runner.Install(forgeVersion.MinecraftVersionName, installer, options, showAd);
    }

    private void showAd()
    {
        //########################AD URL##############################
        try
        {
            Process.Start(new ProcessStartInfo(ForgeAdUrl) { UseShellExecute = true });
        }
        catch
        {
            // ignore when url open failed
        }
        //########################AD URL##############################
    }
}
