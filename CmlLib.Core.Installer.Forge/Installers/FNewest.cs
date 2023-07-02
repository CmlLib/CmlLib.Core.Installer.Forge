using CmlLib.Core.Downloader;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Utils;
using Newtonsoft.Json.Linq;

namespace CmlLib.Core.Installer.Forge.Installers;

/* 1.12.2 - 1.20.* */
public class FNewest : ForgeInstaller
{
    public FNewest(
        MinecraftPath minecraftPath,
        string javaPath,
        IDownloader downloader) :
        base(minecraftPath, javaPath, downloader)
    {

    }

    protected override async Task Install(
        ForgeVersion forgeVersion, 
        string installerDir, 
        string versionName,
        IProgress<DownloadFileChangedEventArgs>? progress)
    {
        var vanillaJarPath = MinecraftPath.GetVersionJarPath(forgeVersion.MinecraftVersionName); // get vanilla jar file

        var installProfilePath = Path.Combine(installerDir, "install_profile.json");
        var installer = JObject.Parse(File.ReadAllText(installProfilePath));
        var installerData = installer["data"] as JObject;
        var mapData = installerData == null ? 
            new Dictionary<string, string?>() : 
            MapProcessorData(installerData, "client", vanillaJarPath, installerDir);

        ExtractMavens(installerDir); //setup maven
        await CheckAndDownloadLibraries(installer["libraries"] as JArray, progress); //install libs
        StartProcessors(installer["processors"] as JArray, mapData);
        await setupFolder(
            forgeVersion.MinecraftVersionName, 
            forgeVersion.ForgeVersionName, 
            versionName,
            installerDir); //copy version.json and forge.jar
    }

    private async Task setupFolder(string mcVersion, string forgeVersion, string versionName, string installDir)
    {
        var versionJsonSource = Path.Combine(installDir, "version.json");
        var versionJsonDest = MinecraftPath.GetVersionJsonPath(versionName);
        IOUtil.CreateDirectoryForFile(versionJsonDest);
        await IOUtil.CopyFileAsync(versionJsonSource, versionJsonDest);

        var jar = Path.Combine(installDir, $"maven\\net\\minecraftforge\\forge\\{mcVersion}-{forgeVersion}\\forge-{mcVersion}-{forgeVersion}.jar");
        if (File.Exists(jar)) //fix 1.17+ 
        {
            var jarPath = MinecraftPath.GetVersionJarPath(versionName);
            IOUtil.CreateDirectoryForFile(jarPath);
            await IOUtil.CopyFileAsync(jar, jarPath);
        }
    }
}
