using CmlLib.Core.Installer.Forge;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Downloader;
using System.ComponentModel;
using CmlLib.Utils;

var httpClient = new HttpClient();
var path = new MinecraftPath(); // use default directory
var launcher = new CMLauncher(path);

// show launch progress to console
void fileChanged(DownloadFileChangedEventArgs e)
{
    Console.WriteLine($"[{e.FileKind.ToString()}] {e.FileName} - {e.ProgressedFileCount}/{e.TotalFileCount}");
}
void progressChanged(object? sender, ProgressChangedEventArgs e)
{
    Console.WriteLine($"{e.ProgressPercentage}%");
}

launcher.FileChanged += fileChanged;
launcher.ProgressChanged += progressChanged;

//Initialize variables with the Minecraft version and the Forge version
var mcVersion = "1.20.1";
var forgeVersion = "47.0.35";

//Initialize MForge
var forge = new MForge(launcher);
forge.FileChanged += fileChanged;
forge.InstallerOutput += (s, e) => Console.WriteLine(e);

var version_name = await forge.Install(mcVersion, forgeVersion);
//var version_name = await forge.Install(mcVersion); // install the recommended forge version for mcVersion
//OR var version_name = forge.Install(mcVersion, forgeVersion).GetAwaiter().GetResult();

//Start MineCraft
var launchOption = new MLaunchOption
{
    MaximumRamMb = 1024,
    Session = MSession.GetOfflineSession("TaiogStudio"),
};

var process = await launcher.CreateProcessAsync(version_name, launchOption);

// print game logs
var processUtil = new ProcessUtil(process);
processUtil.OutputReceived += (s, e) => Console.WriteLine(e);
processUtil.StartWithEvents();
await processUtil.WaitForExitTaskAsync();