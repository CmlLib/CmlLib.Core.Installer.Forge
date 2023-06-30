using CmlLib.Core.Downloader;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge
{

    /* 1.7.10 - 1.11.2 */
    public class FLegacy : ForgeLoader
    {

        private readonly MinecraftPath minecraftPath;
        private readonly string JavaPath;
        private CMLauncher launcher;
        private readonly IDownloader downloader;
        public event DownloadFileChangedHandler? FileChanged;
        public event EventHandler<string>? InstallerOutput;

        public FLegacy(MinecraftPath minecraftPath, string JavaPath, CMLauncher launcher, IDownloader downloader, DownloadFileChangedHandler? FileChanged, EventHandler<string>? InstallerOutput) : base(minecraftPath, JavaPath, launcher, downloader, FileChanged, InstallerOutput)
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
            if (!AlwaysUpdate && Directory.Exists(Path.Combine(minecraftPath.Versions, GetLegacyFolderName(mcVersion, forgeVersion))))
                return $"{GetLegacyFolderName(mcVersion, forgeVersion)}";

            var version_jar = minecraftPath.GetVersionJarPath(mcVersion); // get vanilla jar file
            var install_folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); //create folder in temp

            await Parser.DownloadFile(mcVersion, forgeVersion, install_folder); //download forge version
            File.Copy(Path.Combine(install_folder, "installer.jar"), Path.Combine(install_folder, "version.zip"));
            new FastZip().ExtractZip(Path.Combine(install_folder, "version.zip"), install_folder, null); //unzip version

            var installer = JObject.Parse(File.ReadAllText(Path.Combine(install_folder, "install_profile.json")));
            var version = installer["versionInfo"] as JObject;
            var installerData = installer["data"] as JObject;
            var mapData = installerData == null ? new Dictionary<string, string?>() : mapping(installerData, "client", version_jar, install_folder);
            var version_name = version["id"].ToString();
            var destPath = (installer["install"] as JObject)["path"]?.ToString();
            var universalPath = (installer["install"] as JObject)["filePath"]?.ToString();

            if (string.IsNullOrEmpty(universalPath)) throw new InvalidOperationException("filePath property in installer was null");
            if (string.IsNullOrEmpty(destPath)) throw new InvalidOperationException("path property in installer was null");

            extractUniversal(install_folder, universalPath, destPath); // old installer
            await checkLibraries(installer["libraries"] as JArray); //install libs
            process(installer["processors"] as JArray, mapData, install_folder);
            setupFolderLegacy(mcVersion, forgeVersion, install_folder, version_name, version.ToString()); //copy version.json and forge.jar

            //########################AD URL##############################
            Process.Start(new ProcessStartInfo(Parser.getAdUrl()) { UseShellExecute = true });
            //########################AD URL##############################

            await launcher.GetAllVersionsAsync(); //update version list
            return version_name;
        }

        private void setupFolderLegacy(string mcVersion, string forgeVersion, string install_folder, string version_name, string JVersion)
        {
            string version_folder = Path.Combine(minecraftPath.Versions, version_name);
            var universal_jar = Convert.ToInt32(mcVersion.Split('.')[1]) < 10 ?
                $"{GetLegacyForgeName(mcVersion, forgeVersion)}-universal.jar" :
                $"{GetOldForgeName(mcVersion, forgeVersion)}-universal.jar";

            if (Directory.Exists(version_folder))
                Directory.Delete(version_folder, true); //remove version folder
            Directory.CreateDirectory(version_folder); //create version folder

            File.Copy(Path.Combine(install_folder, universal_jar),
                Path.Combine(version_folder, $"{version_name}.jar")); //copy jar file
            File.WriteAllText(Path.Combine(version_folder, $"{version_name}.json"), JVersion); //write version.json
            Directory.Delete(install_folder, true); //remove temp folder
        }

    }
}
