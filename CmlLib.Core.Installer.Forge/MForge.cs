using CmlLib.Core.Downloader;
using CmlLib.Core.Installer.Forge.Installers;
using CmlLib.Core.Installer.Forge.Versions;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge;

public class MForge
{
    public static readonly string ForgeAdUrl =
        "https://adfoc.us/serve/sitelinks/?id=271228&url=https://maven.minecraftforge.net/";

    private readonly HttpClient _httpClient;
    private readonly MinecraftPath _minecraftPath;
    private readonly CMLauncher _launcher;
    private readonly IDownloader _downloader;
    private readonly IForgeVersionNameResolver _versionNameResolver;

    public string? JavaPath { get; set; }
    public event DownloadFileChangedHandler? FileChanged;
    public event EventHandler<string>? InstallerOutput;

    public MForge(CMLauncher launcher)
    {
        _httpClient = new HttpClient();
        _minecraftPath = launcher.MinecraftPath;
        _launcher = launcher;
        _downloader = new SequenceDownloader();
        _versionNameResolver = new ForgeVersionNameResolver();
    }

    public async Task<string> Install(string mcVersion, bool forceUpdate = false)
    {
        var versionLoader = new ForgeVersionLoader(_httpClient);
        var versions = await versionLoader.GetForgeVersions(mcVersion);
        var bestVersion =
            versions.FirstOrDefault(v => v.IsRecommendedVersion) ??
            versions.FirstOrDefault(v => v.IsLatestVersion) ??
            versions.FirstOrDefault() ??
            throw new InvalidOperationException("Cannot find any version");

        return await Install(bestVersion, forceUpdate);
    }

    public async Task<string> Install(string mcVersion, string forgeVersion, bool forceUpdate = false)
    {
        var versionLoader = new ForgeVersionLoader(_httpClient);
        var versions = await versionLoader.GetForgeVersions(mcVersion);

        var foundVersion = versions.FirstOrDefault(v => v.ForgeVersionName == forgeVersion) ??
            throw new InvalidOperationException("Cannot find version name " + forgeVersion);
        return await Install(foundVersion, forceUpdate);
    }

    public async Task<string> Install(ForgeVersion forgeVersion, bool forceUpdate)
    {
        var versionName = _versionNameResolver.Resolve(
            forgeVersion.MinecraftVersionName,
            forgeVersion.ForgeVersionName);

        if (checkVersionInstalled(versionName) && !forceUpdate)
            return versionName;

        await checkAndDownloadVanillaVersion(forgeVersion.MinecraftVersionName);
        var javaPath = await getJavaPath(forgeVersion.MinecraftVersionName);
        //var javaPath = "java";

        ForgeInstaller installer;
        if (isOldType(forgeVersion.MinecraftVersionName))
            installer = new FLegacy(_minecraftPath, javaPath, _downloader);
        else
            installer = new FNewest(_minecraftPath, javaPath, _downloader);

        var progress = new Progress<DownloadFileChangedEventArgs>(e => FileChanged?.Invoke(e));
        installer.InstallerOutput += (s, e) => InstallerOutput?.Invoke(this, e);
        await installer.Install(forgeVersion, versionName, progress);

        showAd();
        await _launcher.GetAllVersionsAsync();
        return versionName;
    }

    private async Task checkAndDownloadVanillaVersion(string mcVersion)
    {
        if (!File.Exists(_minecraftPath.GetVersionJarPath(mcVersion)))
        {
            var version = await _launcher.GetVersionAsync(mcVersion);
            await _launcher.CheckAndDownloadAsync(version);
        }
    }

    private bool checkVersionInstalled(string versionName)
    {
        //var versions = await _launcher.GetAllVersionsAsync();
        //return versions.Any(v => v.Name == versionName);
        return File.Exists(_minecraftPath.GetVersionJsonPath(versionName));
    }

    private async Task<string> getJavaPath(string versionName)
    {
        if (!string.IsNullOrEmpty(JavaPath))
            return JavaPath;

        var version = await _launcher.GetVersionAsync(versionName);
        var javaPath = _launcher.GetJavaPath(version);
        if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
            throw new InvalidOperationException("Cannot find any java binary. Set java binary path");
        return javaPath;
    }

    private static bool isOldType(string mcVersion) => Convert.ToInt32(mcVersion.Split('.')[1]) < 12 ? true : false;

    private void showAd()
    {
        //########################AD URL##############################
        Process.Start(new ProcessStartInfo(ForgeAdUrl) { UseShellExecute = true });
        //########################AD URL##############################
    }
}
