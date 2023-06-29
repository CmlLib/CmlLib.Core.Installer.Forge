using CmlLib.Core.Downloader;
using CmlLib.Core.Files;
using CmlLib.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge
{
    public abstract class ForgeLoader : Func
    {
        private readonly MinecraftPath minecraftPath;
        private readonly string JavaPath;
        private CMLauncher launcher;
        private readonly IDownloader downloader;
        public event DownloadFileChangedHandler? FileChanged;
        public event EventHandler<string>? InstallerOutput;

        public ForgeLoader(MinecraftPath minecraftPath, string JavaPath, CMLauncher launcher, 
            IDownloader downloader, DownloadFileChangedHandler? FileChanged,
            EventHandler<string>? InstallerOutput)
        {
            this.minecraftPath = minecraftPath;
            this.JavaPath = JavaPath;
            this.downloader = downloader;
            this.launcher = launcher;
            this.FileChanged = FileChanged; 
            this.InstallerOutput = InstallerOutput;
        }

        public void extractMaven(string installerPath)
        {
            var org = Path.Combine(installerPath, "maven");
            if (Directory.Exists(org))
                IOUtil.CopyDirectory(org, minecraftPath.Library, true);
        }

        public void extractUniversal(string installerPath, string universalPath, string destinyName)
        {

            if (string.IsNullOrEmpty(universalPath) || string.IsNullOrEmpty(destinyName))
                return;

            // copy universal library to minecraft
            var universal = Path.Combine(installerPath, universalPath);
            var desPath = PackageName.Parse(destinyName).GetPath();
            var des = Path.Combine(minecraftPath.Library, desPath);

            if (File.Exists(universal))
            {
                var dirPath = Path.GetDirectoryName(des);
                if (!string.IsNullOrEmpty(dirPath))
                    Directory.CreateDirectory(dirPath);
                File.Copy(universal, des, true);
            }
        }

        public Task checkLibraries(JArray? jarr)
        {
            if (jarr == null || jarr.Count == 0)
                return Task.CompletedTask;

            var libs = new List<MLibrary>();
            var parser = new MLibraryParser();
            foreach (var item in jarr)
            {
                var parsedLib = parser.ParseJsonObject((JObject)item);
                if (parsedLib != null)
                    libs.AddRange(parsedLib);
            }

            var fileProgress = new Progress<DownloadFileChangedEventArgs>(
                e => FileChanged?.Invoke(e));

            var libraryChecker = new LibraryChecker();
            var lostLibrary = libraryChecker.CheckFiles(minecraftPath, libs.ToArray(), fileProgress);

            if (lostLibrary == null)
                return Task.CompletedTask;
            downloader.DownloadFiles(lostLibrary, fileProgress, null).GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        public Dictionary<string, string?> mapping(JObject data, string kind,
            string minecraftJar, string installerPath)
        {
            var dataMapping = new Dictionary<string, string?>();
            foreach (var item in data)
            {
                var key = item.Key;
                var value = item.Value?[kind]?.ToString();

                if (string.IsNullOrEmpty(value))
                    continue;

                var fullPath = Mapper.ToFullPath(value, minecraftPath.Library);
                if (fullPath == value)
                {
                    value = value.Trim('/');
                    dataMapping.Add(key, Path.Combine(installerPath, value));
                }
                else
                    dataMapping.Add(key, fullPath);
            }

            dataMapping.Add("SIDE", "CLIENT");
            dataMapping.Add("MINECRAFT_JAR", minecraftJar);

            return dataMapping;
        }

        public void process(JArray? processors, Dictionary<string, string?> mapData, string install_folder)
        {
            if (processors == null || processors.Count == 0)
                return;


            for (int i = 0; i < processors.Count; i++)
            {
                var item = processors[i];

                var outputs = item["outputs"] as JObject;
                if (outputs == null || !checkProcessorOutputs(outputs, mapData))
                    if (item["sides"] == null || (item["sides"] as JArray)[0].ToString() == "client") //skip server side
                        startProcessor(item, mapData, install_folder);

            }
        }

        public bool checkProcessorOutputs(JObject outputs, Dictionary<string, string?> mapData)
        {
            foreach (var item in outputs)
            {
                if (item.Value == null)
                    continue;

                var key = Mapper.Interpolation(item.Key, mapData, true);
                var value = Mapper.Interpolation(item.Value.ToString(), mapData, true);

                if (!File.Exists(key) || !IOUtil.CheckSHA1(key, value))
                    return false;
            }

            return true;
        }

        public void startProcessor(JToken processor, Dictionary<string, string?> mapData, string install_folder)
        {
            var name = processor["jar"]?.ToString();
            if (name == null)
                return;

            // jar
            var jar = PackageName.Parse(name);
            var jarPath = Path.Combine(minecraftPath.Library, jar.GetPath());

            var jarFile = new JarFile(jarPath);
            var jarManifest = jarFile.GetManifest();

            // mainclass
            string? mainClass = null;
            var hasMainclass = jarManifest?.TryGetValue("Main-Class", out mainClass) ?? false;
            if (!hasMainclass || string.IsNullOrEmpty(mainClass))
                return;

            // classpath
            var classpathObj = processor["classpath"];
            var classpath = new List<string>();
            if (classpathObj != null)
            {
                foreach (var libName in classpathObj)
                {
                    var libNameString = libName?.ToString();
                    if (string.IsNullOrEmpty(libNameString))
                        continue;

                    var lib = Path.Combine(minecraftPath.Library,
                        PackageName.Parse(libNameString).GetPath());
                    classpath.Add(lib);
                }
            }
            classpath.Add(jarPath);

            // arg
            var argsArr = processor["args"] as JArray;
            string[]? args = null;
            if (argsArr != null)
            {
                var arrStrs = argsArr.Select(x => x.ToString()).ToArray();
                args = Mapper.Map(arrStrs, mapData, minecraftPath.Library);
            }

            startJava(classpath.ToArray(), mainClass, args, install_folder);
        }

        public void startJava(string[] classpath, string mainClass, string[]? args, string install_folder)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "{INSTALLER}")
                    args[i] = args[i].Replace("{INSTALLER}", Path.Combine(install_folder, "installer.jar"));
            var arg =
                $"-cp {IOUtil.CombinePath(classpath)} " +
                $"{mainClass}";

            if (args != null && args.Length > 0)
                arg += " " + string.Join(" ", args);

            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = JavaPath,
                Arguments = arg.Replace("--side CLIENT", "--side client"), //fix installertools bug
            };

            var p = new ProcessUtil(process);
            p.OutputReceived += (s, e) =>
            InstallerOutput?.Invoke(this, e);
            p.StartWithEvents();
            p.Process.WaitForExit();
        }
    }
}
