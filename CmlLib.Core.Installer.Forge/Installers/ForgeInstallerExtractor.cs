using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge.Installers;

public class ForgeInstallerExtractor : IDisposable
{
    public static Task<ForgeInstallerExtractor> DownloadAndExtractInstaller(
        ForgeVersion version,
        IGameInstaller installer,
        ForgeInstallOptions options)
    {
        return DownloadAndExtract(
            version,
            installer,
            options,
            "installer.jar",
            version.GetInstallerFile()?.DirectUrl
        );
    }

    public static Task<ForgeInstallerExtractor> DownloadAndExtractUniversalInstaller(
        ForgeVersion version,
        IGameInstaller installer,
        ForgeInstallOptions options)
    {
        return DownloadAndExtract(
            version,
            installer,
            options,
            "installer.zip",
            version.GetUniversalFile()?.DirectUrl
        );
    }

    private static async Task<ForgeInstallerExtractor> DownloadAndExtract(
        ForgeVersion version,
        IGameInstaller installer,
        ForgeInstallOptions options,
        string installerFileName,
        string? installerUrl)
    {
        return await DownloadAndExtract(
            version.ForgeVersionName,
            installer,
            options,
            installerFileName,
            installerUrl);
    }

    public static Task<ForgeInstallerExtractor> DownloadAndExtractInstaller(
        ForgeV12VersionArtifact artifact,
        IGameInstaller installer,
        ForgeInstallOptions options)
    {
        return DownloadAndExtract(
            artifact.LoaderVersion,
            installer,
            options,
            "installer.jar",
            artifact.InstallerUrl);
    }

    private static async Task<ForgeInstallerExtractor> DownloadAndExtract(
        string loaderVersion,
        IGameInstaller installer,
        ForgeInstallOptions options,
        string installerFileName,
        string? installerUrl)
    {
        options.CancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(installerUrl))
            throw new InvalidOperationException("The forge version doesn't have installer url");

        var installDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); //create folder in temp
        var installerPath = Path.Combine(installDir, installerFileName);

        var file = new GameFile(loaderVersion)
        {
            Path = installerPath,
            Url = installerUrl,
            Hash = "",
        };

        var extractor = new ForgeInstallerExtractor(installDir);
        try
        {
            await installer.Install([file], options.FileProgress, options.ByteProgress, options.CancellationToken);
            options.CancellationToken.ThrowIfCancellationRequested();
            var zip = new FastZip();
            zip.ExtractZip(installerPath, installDir, null);
            options.CancellationToken.ThrowIfCancellationRequested();
            return extractor;
        }
        catch
        {
            extractor.Dispose();
            throw;
        }
    }

    private ForgeInstallerExtractor(string dir)
    {
        ExtractedDir = dir;
    }

    public string ExtractedDir { get; }

    public Stream OpenInstallerProfile()
    {
        var installProfilePath = Path.Combine(ExtractedDir, "install_profile.json");
        if (!File.Exists(installProfilePath))
            throw new InvalidOperationException("The installer doesn't contain install_profile.json");
        return File.OpenRead(installProfilePath);
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue)
            return;

        disposedValue = true;
        try
        {
            Directory.Delete(ExtractedDir, true);
        }
        catch (IOException error)
        {
            Debug.WriteLine(error);
        }
        catch (UnauthorizedAccessException error)
        {
            Debug.WriteLine(error);
        }
    }

    ~ForgeInstallerExtractor()
    {
         Dispose(disposing: false);
    }

    public void Dispose()
    {
        try
        {
            Dispose(disposing: true);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }
}
