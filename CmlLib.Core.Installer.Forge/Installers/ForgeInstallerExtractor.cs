﻿using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;
using ICSharpCode.SharpZipLib.Zip;

namespace CmlLib.Core.Installer.Forge.Installers;

public class ForgeInstallerExtractor : IDisposable
{
    public static async Task<ForgeInstallerExtractor> DownloadAndExtractInstaller(ForgeVersion version, IGameInstaller installer, ForgeInstallOptions options)
    {
        var installDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); //create folder in temp
        var installerJar = Path.Combine(installDir, "installer.jar");
        var installerUrl = version.GetInstallerFile()?.DirectUrl;
        if (string.IsNullOrEmpty(installerUrl))
            throw new InvalidOperationException("The forge version doesn't have installer url");

        var file = new GameFile(version.ForgeVersionName)
        {
            Path = installerJar,
            Url = installerUrl,
            Hash = "",
        };
        await installer.Install([file], options.FileProgress, options.ByteProgress, options.CancellationToken);

        var zip = new FastZip();
        zip.ExtractZip(installerJar, installDir, null);
        return new ForgeInstallerExtractor(installDir);
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
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed objects
            }

            Directory.Delete(ExtractedDir, true);
            disposedValue = true;
        }
    }

    ~ForgeInstallerExtractor()
    {
         Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
