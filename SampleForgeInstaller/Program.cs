using CmlLib.Core.Installer.Forge;
using CmlLib.Core;
using CmlLib.Core.Auth;

var path = new MinecraftPath("C:\\Users\\semoy\\minecraft"); // use default directory

var launcher = new CMLauncher(path);

// show launch progress to console
launcher.FileChanged += (e) => Console.WriteLine($"[{e.FileKind.ToString()}] {e.FileName} - {e.ProgressedFileCount}/{e.TotalFileCount}");
launcher.ProgressChanged += (s, e) => Console.WriteLine($"{e.ProgressPercentage}%");

//Initialize variables with the Minecraft version and the Forge version
var mcVersion = "1.20.1";
var forgeVersion = "47.0.35";

//Initialize MForge
var forge = new MForge(path, launcher);
var version_name = await forge.Install(mcVersion, forgeVersion); //Use await in the asynchronous method
//OR var version_name = forge.Install(mcVersion, forgeVersion).GetAwaiter().GetResult();

//Start MineCraft
var launchOption = new MLaunchOption
{
    MaximumRamMb = 1024,
    Session = MSession.GetOfflineSession("TaiogStudio"),
};

var process = launcher.CreateProcess(version_name, launchOption);
process.Start();

//var httpClient = new HttpClient();
//var forgeVersionLoader = new ForgeVersionLoader(httpClient);
//var result = await forgeVersionLoader.GetForgeVersions("1.12.2");
//foreach (var v in result)
//{
//    Console.WriteLine("MinecraftVersionName : " + v.MinecraftVersionName);
//    Console.WriteLine("ForgeVersionName : " + v.ForgeVersionName);
//    Console.WriteLine("Time : " + v.Time);
//    if (v.Files != null)
//    {
//        foreach (var f in v.Files)
//        {
//            Console.WriteLine("Type : " + f.Type);
//            Console.WriteLine("AdUrl : " + f.AdUrl);
//            Console.WriteLine("DirectUrl : " + f.DirectUrl);
//            Console.WriteLine("MD5 : " + f.MD5);
//            Console.WriteLine("SHA1 : " + f.SHA1);
//        }
//    }
//    Console.WriteLine();
//}