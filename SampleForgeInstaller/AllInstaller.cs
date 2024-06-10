﻿using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;

namespace SampleForgeInstaller;

internal class AllInstaller
{
    MinecraftLauncher _launcher;
    ForgeInstaller _forge;

    public AllInstaller()
    {
        _launcher = new MinecraftLauncher(new MinecraftPath());
        _forge = new ForgeInstaller(_launcher);
    }

    public async Task InstallAll()
    {
        var versions = new string[]
        {
            "1.20.6", //ok
            "1.20.1", // ok
            "1.20", // ok
            "1.19.4", // ok
            "1.19.3", // ok
            "1.19.2", // ok
             "1.19.1", // ok
            "1.19", // ok
            "1.18.2", // ok
            "1.18.1", // ok
            "1.18", // ok
            "1.17.1", // ok
            "1.16.5", // ok
            "1.16.4", // ok
            "1.16.3", // ok
            "1.16.2", // ok
            "1.16.1", // ok
            "1.15.2", // ok
            "1.15.1", // ok
            "1.15", // ok
            "1.14.4", // ok
            "1.14.3", // ok
            "1.14.2", // ok
            "1.13.2", // ok
            "1.12.2", // ok
            "1.12.1", // ok
            "1.12", // ok
            "1.11.2", // ok
            "1.11", // ok
            "1.10.2", // ok
            "1.10", // ok
            "1.9.4", // ok
            "1.9", // ok
            "1.8.9", // ok
            "1.8.8", // ok
            "1.7.10", // ok
            "1.7.10_pre4", // cannot find 1.7.10_pre4
            "1.7.2", // crash
            "1.6.4", // crash
            "1.6.3", // crash
            "1.6.2", // crash
            "1.6.1", // crash, wrong version name
            "1.5.2" // crash
        };

        foreach (var version in versions.Reverse())
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
        var versionName = await _forge.Install(mcVersion, new ForgeInstallOptions
        {
            FileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(fileChanged),
            ByteProgress = new SyncProgress<ByteProgress>(progressChanged),
            InstallerOutput = new SyncProgress<string>(logOutput),
            SkipIfAlreadyInstalled = false
        });
        var process = await _launcher.CreateProcessAsync(versionName, new MLaunchOption
        {
            Session = MSession.CreateOfflineSession("tester123")
        });

        var processUtil = new ProcessWrapper(process);
        processUtil.OutputReceived += (s, e) => logOutput(e);
        processUtil.StartWithEvents();
        await Task.WhenAny(Task.Delay(30000), processUtil.WaitForExitTaskAsync());
        if (processUtil.Process.HasExited)
            throw new Exception("Process was dead!");
        else
            processUtil.Process.Kill();
    }

    private void logOutput(string e)
    {
        Console.WriteLine(e);
    }

    private void fileChanged(InstallerProgressChangedEventArgs e)
    {
        Console.WriteLine($"[{e.EventType}][{e.ProgressedTasks}/{e.TotalTasks}] {e.Name}");
    }

    private void progressChanged(ByteProgress e)
    {
        Console.WriteLine(e.ToRatio() * 100 + "%");
    }
}
