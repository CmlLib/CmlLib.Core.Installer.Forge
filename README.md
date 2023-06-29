# CmlLib.Core.Installer.Forge
## Minecraft Forge Installer
<img src='https://raw.githubusercontent.com/CmlLib/CmlLib.Core/master/icon.png' width=128>

This is the official package for installing Forge for the CmlLib.Core library. 
## Features 
* Forge Developer Support! After successfully installing the Forge version, the Forge advertising page will automatically open for you.
* Automatic change of links to install Forge
* Installing Forge 1.7.10
* Installing a legacy version of the type (1.8-1.9.4)
* Installing the old type version (1.10-1.11.2)
* Installing a new type of version (1.12 - 1.16.5)
* Installing the newest version of the type (1.16.5 - 1.20.*)
* Automatic installation of the Vanilla version of Minecraft before installing Forge
* Skipping the Forge re-installation
## Install

Install the [CmlLib.Core Nuget package](https://www.nuget.org/packages/CmlLib.Core)

or download the DLL files in [Releases](https://github.com/AlphaBs/CmlLib.Core/releases) and add references to them in your project.

Write this at the top of your source code:
```csharp
using CmlLib.Core.Installer.Forge;
using CmlLib.Core;
using CmlLib.Core.Auth;
```
## Quick start
```csharp
var session = MSession.GetOfflineSession("USERNAME"); //https://github.com/CmlLib/CmlLib.Core/wiki/Login-and-Sessions
//var path = new MinecraftPath("game_directory_path");
var path = new MinecraftPath(); // use default directory

var launcher = new CMLauncher(path);

// show launch progress to console
launcher.FileChanged += (e) =>onsole.WriteLine($"[{e.FileKind.ToString()}] {e.FileName} - {e.ProgressedFileCount}/{e.TotalFileCount}");
launcher.ProgressChanged += (s, e) => Console.WriteLine($"{e.ProgressPercentage}%");

//Initialize variables with the Minecraft version and the Forge version
var mcVersion = "1.12.2";
var forgeVersion = "14.23.5.2860";

//Initialize MForge
var forge = new IForge(path, launcher);
var version_name = await forge.Install(mcVersion, forgeVersion); //OR var version_name = forge.Install(mcVersion, forgeVersion).GetAwaiter().GetResult();

//Start MineCraft
var launchOption = new MLaunchOption
{
  MaximumRamMb = 1024,
  Session = MSession.GetOfflineSession("TaigoStudio"),
};

var process = launcher.CreateProcess(version_name, launchOption);
process.Start();
```
You can disable the quick launch feature if the version is already installed: 
```csharp
var forge = new IForge(path, launcher, true);
```
