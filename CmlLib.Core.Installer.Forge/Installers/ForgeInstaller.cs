using CmlLib.Core.Downloader;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge.Installers;

public abstract class ForgeInstaller : IForgeInstaller
{
    public string VersionName { get; }
    public ForgeVersion ForgeVersion { get; }
    public event DownloadFileChangedHandler? FileChanged;
    public event EventHandler<string>? InstallerOutput;

    private readonly IProgress<DownloadFileChangedEventArgs> _progress;        
    private readonly FastZip _zip = new FastZip();
    private ForgeInstallOptions? _options;
    protected ForgeInstallOptions InstallOptions
    {
        get => _options ?? throw new InvalidOperationException();
        set => _options = value;
    }

    public ForgeInstaller(string versionName, ForgeVersion forgeVersion)
    {
        VersionName = versionName;
        ForgeVersion = forgeVersion;
        _progress = new Progress<DownloadFileChangedEventArgs>(e => FileChanged?.Invoke(e));
    }

    public async Task Install(ForgeInstallOptions options)
    {
        InstallOptions = options;
        var installerDir = await downloadAndExtractInstaller();
        await Install(installerDir);
        Directory.Delete(installerDir, true); //remove temp folder
    }

    protected abstract Task Install(string installerDir);

    private async Task<string> downloadAndExtractInstaller()
    {
        var installDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); //create folder in temp
        var installerJar = Path.Combine(installDir, "installer.jar");
        var installerUrl = ForgeVersion.GetInstallerFile()?.DirectUrl;
        if (string.IsNullOrEmpty(installerUrl))
            throw new InvalidOperationException("The forge version doesn't have installer url");

        await InstallOptions.Downloader.DownloadFiles(
            new[] { new DownloadFile(installerJar, installerUrl) }, 
            _progress, null);

        _zip.ExtractZip(installerJar, installDir, null);
        return installDir;
    }

    protected void ExtractMavens(string installerPath)
    {
        var org = Path.Combine(installerPath, "maven");
        if (Directory.Exists(org))
            IOUtil.CopyDirectory(org, InstallOptions.MinecraftPath.Library, true);
    }

    protected void ExtractUniversal(string installerPath, string universalPath, string destinyName)
    {
        if (string.IsNullOrEmpty(universalPath) || string.IsNullOrEmpty(destinyName))
            return;

        // copy universal library to minecraft
        var universal = Path.Combine(installerPath, universalPath);
        var desPath = PackageName.Parse(destinyName).GetPath();
        var des = Path.Combine(InstallOptions.MinecraftPath.Library, desPath);

        if (File.Exists(universal))
        {
            var dirPath = Path.GetDirectoryName(des);
            if (!string.IsNullOrEmpty(dirPath))
                Directory.CreateDirectory(dirPath);
            File.Copy(universal, des, true);
        }
    }

    protected async Task CheckAndDownloadLibraries(JArray? jarr)
    {
        if (jarr == null || jarr.Count == 0)
            return;

        var libs = new List<MLibrary>();
        var parser = new MLibraryParser();
        foreach (var item in jarr)
        {
            var parsedLib = parser.ParseJsonObject((JObject)item);
            if (parsedLib != null)
                libs.AddRange(parsedLib);
        }

        var libraryChecker = new LibraryChecker();
        var lostLibrary = libraryChecker.CheckFiles(
            InstallOptions.MinecraftPath, libs.ToArray(), _progress);

        if (lostLibrary != null)
            await InstallOptions.Downloader.DownloadFiles(lostLibrary, _progress, null);
    }

    protected Dictionary<string, string?> MapProcessorData(
        JObject data, string kind, string minecraftJar, string installDir)
    {
        var dataMapping = new Dictionary<string, string?>();
        foreach (var item in data)
        {
            var key = item.Key;
            var value = item.Value?[kind]?.ToString();

            if (string.IsNullOrEmpty(value))
                continue;

            var fullPath = Mapper.ToFullPath(value, InstallOptions.MinecraftPath.Library);
            if (fullPath == value)
            {
                value = value.Trim('/');
                dataMapping.Add(key, Path.Combine(installDir, value));
            }
            else
                dataMapping.Add(key, fullPath);
        }

        dataMapping.Add("SIDE", "client");
        dataMapping.Add("MINECRAFT_JAR", minecraftJar);
        dataMapping.Add("INSTALLER", Path.Combine(installDir, "installer.jar"));

        return dataMapping;
    }

    protected void StartProcessors(JArray? processors, Dictionary<string, string?> mapData)
    {
        if (processors == null || processors.Count == 0)
            return;

        for (int i = 0; i < processors.Count; i++)
        {
            var item = processors[i];

            var outputs = item["outputs"] as JObject;
            if (outputs == null || !checkProcessorOutputs(outputs, mapData))
            {
                var sides = item["sides"] as JArray;
                if (sides == null || sides.FirstOrDefault()?.ToString() == "client") //skip server side
                    startProcessor(item, mapData);
            }
        }
    }

    private bool checkProcessorOutputs(JObject outputs, Dictionary<string, string?> mapData)
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

    private void startProcessor(JToken processor, Dictionary<string, string?> mapData)
    {
        var name = processor["jar"]?.ToString();
        if (name == null)
            return;

        // jar
        var jar = PackageName.Parse(name);
        var jarPath = Path.Combine(InstallOptions.MinecraftPath.Library, jar.GetPath());

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

                var lib = Path.Combine(InstallOptions.MinecraftPath.Library,
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
            args = Mapper.Map(arrStrs, mapData, InstallOptions.MinecraftPath.Library);
        }

        startJava(classpath.ToArray(), mainClass, args);
    }

    private void startJava(string[] classpath, string mainClass, string[]? args)
    {
        if (string.IsNullOrEmpty(InstallOptions.JavaPath))
            throw new InvalidOperationException("JavaPath was empty");

        var arg =
            $"-cp {IOUtil.CombinePath(classpath)} " +
            $"{mainClass}";

        if (args != null && args.Length > 0)
            arg += " " + string.Join(" ", args);

        var process = new Process();
        process.StartInfo = new ProcessStartInfo()
        {
            FileName = InstallOptions.JavaPath,
            Arguments = arg,
        };

        var p = new ProcessUtil(process);
        p.OutputReceived += (s, e) =>
        InstallerOutput?.Invoke(this, e);
        p.StartWithEvents();
        p.Process.WaitForExit();
    }
}
