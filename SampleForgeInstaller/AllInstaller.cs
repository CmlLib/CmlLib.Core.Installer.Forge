using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Utils;

namespace SampleForgeInstaller;

internal class AllInstaller
{
    CMLauncher _launcher;
    MForge _forge;

    public AllInstaller()
    {
        _launcher = new CMLauncher(new MinecraftPath());
        _launcher.ProgressChanged += progressChanged;
        _launcher.FileChanged += fileChanged;
        _launcher.LogOutput += logOutput;

        _forge = new MForge(_launcher);
        _forge.FileChanged += fileChanged;
        _forge.InstallerOutput += logOutput;
    }

    public async Task InstallAll()
    {
        var versions = new string[]
        {
            //"1.20.1",
            //"1.20",
            "1.19.4",
            "1.19.3",
            "1.19.2",
            "1.19.1",
            "1.19",
            "1.18.2",
            "1.18.1",
            "1.18",
            "1.17.1",
            "1.16.5",
            "1.16.4",
            "1.16.3",
            "1.16.2",
            "1.16.1",
            "1.15.2",
            "1.15.1",
            "1.15",
            "1.14.4",
            "1.14.3",
            "1.14.2",
            "1.13.2",
            "1.12.2",
            "1.12.1",
            "1.12",
            "1.11.2",
            "1.11",
            "1.10.2",
            "1.10",
            "1.9.4",
            "1.9",
            "1.8.9",
            "1.8.8",
            "1.7.10",
            "1.7.10_pre4",
            "1.7.2",
            "1.6.4",
            "1.6.3",
            "1.6.2",
            "1.6.1",
            "1.5.2"
        };

        foreach (var version in versions)
        {
            try
            {
                await InstallAndLaunch(version);
            }
            catch(Exception ex)
            {
                throw;
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                Console.ReadLine();
            }
        }
    }

    public async Task InstallAndLaunch(string mcVersion)
    {
        Console.WriteLine("Minecraft: " + mcVersion);
        var versionName = await _forge.Install(mcVersion, true);
        var process = await _launcher.CreateProcessAsync(versionName, new MLaunchOption
        {
            Session = MSession.GetOfflineSession("tester123")
        });

        var processUtil = new ProcessUtil(process);
        processUtil.OutputReceived += logOutput;
        processUtil.StartWithEvents();
        await Task.WhenAny(Task.Delay(30000), processUtil.WaitForExitTaskAsync());
        if (processUtil.Process.HasExited)
            throw new Exception("Process was dead!");
        else
            processUtil.Process.Kill();
    }

    private void logOutput(object? sender, string e)
    {
        Console.WriteLine(e);
    }

    private void fileChanged(CmlLib.Core.Downloader.DownloadFileChangedEventArgs e)
    {
        Console.WriteLine($"[{e.FileKind.ToString()}] {e.FileName} - {e.ProgressedFileCount}/{e.TotalFileCount}");
    }

    private void progressChanged(object? sender, System.ComponentModel.ProgressChangedEventArgs e)
    {
        Console.WriteLine(e.ProgressPercentage + "%");
    }
}
