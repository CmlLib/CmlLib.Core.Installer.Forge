using CmlLib.Core.Downloader;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge
{
    /* 1.12.2 - 1.20.* */
    public class FNewest : ForgeLoader
    {
        private readonly MinecraftPath minecraftPath;
        private readonly string JavaPath;
        private CMLauncher launcher;
        private readonly IDownloader downloader;
        public event DownloadFileChangedHandler? FileChanged;
        public event EventHandler<string>? InstallerOutput;

        public FNewest(MinecraftPath minecraftPath, string JavaPath, CMLauncher launcher, IDownloader downloader, DownloadFileChangedHandler? FileChanged, EventHandler<string>? InstallerOutput) : base(minecraftPath, JavaPath, launcher, downloader, FileChanged, InstallerOutput)
        {
            this.minecraftPath = minecraftPath;
            this.JavaPath = JavaPath;
            this.downloader = downloader;
            this.launcher = launcher;
            this.FileChanged = FileChanged;
            this.InstallerOutput = InstallerOutput;
        }
        public async Task<string> Install(string mcVersion, string forgeVersion, bool AlwaysUpdate = false)
        {
            if (!AlwaysUpdate && Directory.Exists(Path.Combine(minecraftPath.Versions, GetForgeName(mcVersion, forgeVersion))))
                return GetForgeName(mcVersion, forgeVersion); //the version is already installed

            var version_jar = minecraftPath.GetVersionJarPath(mcVersion); // get vanilla jar file
            var install_folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); //create folder in temp
            if (!File.Exists(version_jar))
                await launcher.CheckAndDownloadAsync(launcher.GetVersion(mcVersion)); //install vanilla version

            await Parser.DownloadFile(mcVersion, forgeVersion, install_folder); //download forge version
            File.Copy(Path.Combine(install_folder, "installer.jar"), Path.Combine(install_folder, "version.zip"));
            new FastZip().ExtractZip(Path.Combine(install_folder, "version.zip"), install_folder, null); //unzip version

            var version = JObject.Parse(File.ReadAllText(Path.Combine(install_folder, "version.json")));
            var installer = JObject.Parse(File.ReadAllText(Path.Combine(install_folder, "install_profile.json")));
            var installerData = installer["data"] as JObject;
            var mapData = installerData == null ? new Dictionary<string, string?>() : mapping(installerData, "client", version_jar, install_folder);

            extractMaven(install_folder); //setup maven
            await checkLibraries(installer["libraries"] as JArray); //install libs
            process(installer["processors"] as JArray, mapData, install_folder);
            setupFolder(mcVersion, forgeVersion, install_folder, version.ToString()); //copy version.json and forge.jar

            //########################AD URL##############################
            Process.Start(Parser.getAdUrl()); //We support Forge developers!
            //########################AD URL##############################

            await launcher.GetAllVersionsAsync(); //update version list
            return GetForgeName(mcVersion, forgeVersion);
        }


        private void setupFolder(string mcVersion, string forgeVersion, string install_folder, string JVersion)
        {
            string version_folder = Path.Combine(minecraftPath.Versions, GetForgeName(mcVersion, forgeVersion));
            if (Directory.Exists(version_folder))
                Directory.Delete(version_folder, true); //remove version folder
            Directory.CreateDirectory(version_folder); //create version folder
            File.WriteAllText(Path.Combine(version_folder, $"{GetForgeName(mcVersion, forgeVersion)}.json"), JVersion); //write version.json
            var jar = Path.Combine(install_folder, $"maven\\net\\minecraftforge\\forge\\{mcVersion}-{forgeVersion}\\forge-{mcVersion}-{forgeVersion}.jar");
            if (File.Exists(jar)) //fix 1.17+ errors
                File.Copy(jar, Path.Combine(version_folder, $"{GetForgeName(mcVersion, forgeVersion)}.jar")); //copy jar file
            Directory.Delete(install_folder, true); //remove temp folder
        }

    }
}
