using CmlLib.Core.Installer.Forge;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;
using SampleForgeInstaller;

var tester = new AllInstaller();
await tester.InstallAll();
return;

var path = new MinecraftPath(); // use default directory
var launcher = new MinecraftLauncher(path);

// show launch progress to console
var fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
    Console.WriteLine($"[{e.EventType}][{e.ProgressedTasks}/{e.TotalTasks}] {e.Name}"));
var byteProgress = new SyncProgress<ByteProgress>(e =>
    Console.WriteLine(e.ToRatio() * 100 + "%"));
var installerOutput = new SyncProgress<string>(Console.WriteLine);

//Initialize variables with the Minecraft version and the Forge version

var mcVersion = "1.5.2";
var forgeVersion = "7.8.1.738";

// var mcVersion = "1.6.1";
// var forgeVersion = "8.9.0.775";

// var mcVersion = "1.6.2";
// var forgeVersion = "9.10.1.871";

// var mcVersion = "1.6.4";
// var forgeVersion = "9.11.1.1345";

// var mcVersion = "1.7.2";
// var forgeVersion = "10.12.2.1161";

//Initialize MForge
var forge = new ForgeInstaller(launcher);

var version_name = await forge.Install(mcVersion, forgeVersion, new ForgeInstallOptions
{
    FileProgress = fileProgress,
    ByteProgress = byteProgress,
    InstallerOutput = installerOutput,
});
//var version_name = await forge.Install(mcVersion); // install the recommended forge version for mcVersion
//OR var version_name = forge.Install(mcVersion, forgeVersion).GetAwaiter().GetResult();

//Start Minecraft
var launchOption = new MLaunchOption
{
    MaximumRamMb = 1024,
    Session = MSession.CreateOfflineSession("TaiogStudio"),
    ExtraJvmArguments = new []
    {
        new MArgument("-Dfml.ignoreInvalidMinecraftCertificates=true"),
    }
};

var process = await launcher.BuildProcessAsync(version_name, launchOption);

// print game logs
var processUtil = new ProcessWrapper(process);
processUtil.OutputReceived += (s, e) => Console.WriteLine(e);
processUtil.StartWithEvents();
await processUtil.WaitForExitTaskAsync();