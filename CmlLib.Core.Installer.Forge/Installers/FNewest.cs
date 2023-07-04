using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Utils;
using Newtonsoft.Json.Linq;

namespace CmlLib.Core.Installer.Forge.Installers;

/* 1.12.2 - 1.20.* */
public class FNewest : ForgeInstaller
{
    public FNewest(string versionName, ForgeVersion forgeVersion) : base(versionName, forgeVersion)
    {
    }

    protected override async Task Install(string installerDir)
    {
        var vanillaJarPath = InstallOptions.MinecraftPath.GetVersionJarPath(ForgeVersion.MinecraftVersionName); // get vanilla jar file

        var installProfilePath = Path.Combine(installerDir, "install_profile.json");
        var installer = JObject.Parse(File.ReadAllText(installProfilePath));
        var installerData = installer["data"] as JObject;
        var mapData = installerData == null ? 
            new Dictionary<string, string?>() : 
            MapProcessorData(installerData, "client", vanillaJarPath, installerDir);

        ExtractMavens(installerDir); //setup maven
        await CheckAndDownloadLibraries(installer["libraries"] as JArray); //install libs
        StartProcessors(installer["processors"] as JArray, mapData);
        await setupFolder(installerDir); //copy version.json and forge.jar
    }

    private async Task setupFolder(string installerDir)
    {
        var versionJsonSource = Path.Combine(installerDir, "version.json");
        var versionJsonDest = InstallOptions.MinecraftPath.GetVersionJsonPath(VersionName);
        IOUtil.CreateDirectoryForFile(versionJsonDest);
        await IOUtil.CopyFileAsync(versionJsonSource, versionJsonDest);

        var m = ForgeVersion.MinecraftVersionName;
        var f = ForgeVersion.ForgeVersionName;
        var jar = Path.Combine(installerDir, $"maven/net/minecraftforge/forge/{m}-{f}/forge-{m}-{f}.jar");
        if (File.Exists(jar)) //fix 1.17+ 
        {
            var jarPath = InstallOptions.MinecraftPath.GetVersionJarPath(VersionName);
            IOUtil.CreateDirectoryForFile(jarPath);
            await IOUtil.CopyFileAsync(jar, jarPath);
        }
    }
}
