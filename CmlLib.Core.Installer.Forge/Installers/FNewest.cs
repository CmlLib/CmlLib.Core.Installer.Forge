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
        var installer = await ReadInstallerProfile(installerDir);
        extractMavens(installerDir); //setup maven
        await CheckAndDownloadLibraries(installer["libraries"] as JArray); //install libs
        await MapAndStartProcessors(installer, installerDir);
        await setupFolder(installerDir); //copy version.json and forge.jar
    }

    private void extractMavens(string installerPath)
    {
        var org = Path.Combine(installerPath, "maven");
        if (Directory.Exists(org))
            IOUtil.CopyDirectory(org, InstallOptions.MinecraftPath.Library, true);
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
