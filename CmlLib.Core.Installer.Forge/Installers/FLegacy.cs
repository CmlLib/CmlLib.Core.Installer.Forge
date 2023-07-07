using CmlLib.Core.Downloader;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;

namespace CmlLib.Core.Installer.Forge.Installers;

/* 1.7.10 - 1.11.2 */
public class FLegacy : ForgeInstaller
{
    public FLegacy(string versionName, ForgeVersion forgeVersion) : 
        base(versionName, forgeVersion)
    {
    }

    protected override async Task Install(string installerDir)
    {
        var installProfile = await ReadInstallerProfile(installerDir);

        var version = (installProfile["versionInfo"] as JObject) ?? 
            throw new InvalidOperationException("no versionInfo property");

        var versionId = version?["id"]?.ToString();
        if (string.IsNullOrEmpty(versionId))
            throw new InvalidOperationException("install_profile.json does not have id property");

        await extractUniversal(installerDir, installProfile); // old installer
        await CheckAndDownloadLibraries(installProfile["libraries"] as JArray); //install libs
        await setupFolderLegacy(version!.ToString()); //copy version.json and forge.jar
    }

    private async Task extractUniversal(string installerDir, JObject installProfile)
    {
        var destPath = (installProfile["install"] as JObject)?["path"]?.ToString();
        var universalPath = (installProfile["install"] as JObject)?["filePath"]?.ToString();

        if (string.IsNullOrEmpty(universalPath))
            throw new InvalidOperationException("filePath property in installer was null");
        if (string.IsNullOrEmpty(destPath))
            throw new InvalidOperationException("path property in installer was null");

        await extractUniversal(installerDir, universalPath, destPath);
    }

    private async Task extractUniversal(string installerPath, string universalPath, string destName)
    {
        // copy universal library to minecraft
        var universal = Path.Combine(installerPath, universalPath);
        var desPath = PackageName.Parse(destName).GetPath();
        var des = Path.Combine(InstallOptions.MinecraftPath.Library, desPath);
        var jarPath = InstallOptions.MinecraftPath.GetVersionJarPath(VersionName);

        if (File.Exists(universal))
        {
            IOUtil.CreateDirectoryForFile(jarPath);
            IOUtil.CreateDirectoryForFile(des);
            await IOUtil.CopyFileAsync(universal, des);
            await IOUtil.CopyFileAsync(universal, jarPath);
        }
    }

    private async Task setupFolderLegacy(string versionContent)
    {
        var jsonPath = InstallOptions.MinecraftPath.GetVersionJsonPath(VersionName);
        IOUtil.CreateDirectoryForFile(jsonPath);
        await IOUtil.WriteFileAsync(jsonPath, versionContent); 
    }
}
