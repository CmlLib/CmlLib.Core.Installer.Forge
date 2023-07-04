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
        var vanillaJarPath = InstallOptions.MinecraftPath.GetVersionJarPath(ForgeVersion.MinecraftVersionName); // get vanilla jar file

        var installProfilePath = Path.Combine(installerDir, "install_profile.json");
        if (!File.Exists(installProfilePath))
            throw new InvalidOperationException("The installer does not have install_profile.json file");

        var installProfileContent = File.ReadAllText(installProfilePath);
        var installProfile = JObject.Parse(installProfileContent);

        var version = installProfile["versionInfo"] as JObject;
        var versionId = version?["id"]?.ToString();
        if (string.IsNullOrEmpty(versionId))
            throw new InvalidOperationException("install_profile.json does not have id property");

        var installerData = installProfile["data"] as JObject;
        var mapData = installerData == null ? 
            new Dictionary<string, string?>() : 
            MapProcessorData(installerData, "client", vanillaJarPath, installerDir);

        var destPath = (installProfile["install"] as JObject)?["path"]?.ToString();
        var universalPath = (installProfile["install"] as JObject)?["filePath"]?.ToString();

        if (string.IsNullOrEmpty(universalPath)) 
            throw new InvalidOperationException("filePath property in installer was null");
        if (string.IsNullOrEmpty(destPath)) 
            throw new InvalidOperationException("path property in installer was null");

        ExtractUniversal(installerDir, universalPath, destPath); // old installer
        await CheckAndDownloadLibraries(installProfile["libraries"] as JArray); //install libs
        StartProcessors(installProfile["processors"] as JArray, mapData);
        setupFolderLegacy(versionId, Path.Combine(installerDir, universalPath), version!.ToString()); //copy version.json and forge.jar
    }

    private void setupFolderLegacy(string versionName, string universalJarPath, string versionContent)
    {
        var jarPath = InstallOptions.MinecraftPath.GetVersionJarPath(versionName);
        var jsonPath = InstallOptions.MinecraftPath.GetVersionJsonPath(versionName);

        IOUtil.CreateDirectoryForFile(jarPath);
        IOUtil.CreateDirectoryForFile(jsonPath);
        if (File.Exists(universalJarPath))
            File.Copy(universalJarPath, jarPath, true);
        File.WriteAllText(jsonPath, versionContent); //write version.json
    }
}
