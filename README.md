# CmlLib.Core.Installer.Forge

## Minecraft Forge Installer
<img src='https://raw.githubusercontent.com/CmlLib/CmlLib.Core/master/icon.png' width=128>

Forge Installer for [CmlLib.Core](https://github.com/CmlLib/CmlLib.Core)

## Features 
* Forge Developer Support! After successfully installing the Forge version, the Forge advertising page will automatically open for you.
* Automatic change of links to install Forge
* Automatic installation of the Vanilla version of Minecraft before installing Forge
* Skipping the Forge re-installation

## Supported Forge Versions

**1.7.10 ~ 1.20.1** forge versions was successfully tested. [All test results](https://cmllib.github.io/CmlLib.Core-wiki/en/installer.forge/supported-versions/)

## Install

Install the [CmlLib.Core.Installer.Forge Nuget package](https://www.nuget.org/packages/CmlLib.Core.Installer.Forge)

or download the nupkg in [Releases](https://github.com/CmlLib/CmlLib.Core.Installer.Forge/releases) and add references to them in your project.

## [Documentation](https://cmllib.github.io/CmlLib.Core-wiki/en/installer.forge/)

[Usages and Examples](https://cmllib.github.io/CmlLib.Core-wiki/en/installer.forge/)

## NeoForge API (in development)

Forge and NeoForge have separate version models: `ForgeVersion` and `NeoForgeVersion`.
NeoForge discovery preserves all version names from the NeoForged Maven catalog,
including alpha, snapshot, and arbitrary suffixes, and returns them in reverse
server response order without sorting. If a release name cannot be mapped to a
Minecraft version, discovery throws `FormatException` containing that name. Minecraft `1.21.0`
is normalized to `1.21`, and calendar releases such as `26.1` and `26.1.2` are mapped
separately.

```csharp
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.Forge.Versions;

var launcher = new MinecraftLauncher(new MinecraftPath("./minecraft"));
var installer = new NeoForgeInstaller(launcher);
var versions = await installer.GetNeoForgeVersions("1.21.1"); // IReadOnlyList<NeoForgeVersion>
// GetNeoForgeVersions() returns releases for all Minecraft versions.
// The Minecraft version overload filters that list with Where.

// Choose the first release in reverse server order, or specify an exact build.
var installedId = await installer.Install("1.21.1", new ForgeInstallOptions());
// var installedId = await installer.Install("1.21.1", "21.1.252", new ForgeInstallOptions());

// A known model can be installed without querying the catalog.
// var installedId = await installer.Install(
//     new NeoForgeVersion("1.21.1", "21.1.252"), new ForgeInstallOptions());
```

`IForgeInstaller` exposes only `VersionName` and `Install`; version models are not
part of the common interface. The legacy Forge implementations retain their own
`ForgeVersion` properties. `ForgeV12Installer` accepts only `ForgeV12VersionArtifact`;
`ForgeInstallerVersionMapper` converts `ForgeVersion` into that artifact before
constructing the installer. The former `IForgeInstaller.ForgeVersion` property and
the `ForgeV12Installer` constructor and property using `ForgeVersion` are removed.

`NeoForgeInstaller` handles discovery and version selection. `NeoForgeVersionMapper`
parses catalog names into the data-only `NeoForgeVersion` record and normalizes
Minecraft versions. `NeoForgeInstallerVersionMapper` converts that record into
`ForgeV12VersionArtifact`; its `CreateInstaller` method creates `ForgeV12Installer`
from the artifact. Both Forge and NeoForge use this engine for installer profiles,
libraries, processors, and optional embedded version JARs.

An internal `InstallerRunner` shares the installed-version check, vanilla preparation,
Java selection, loader installation, and version-list refresh between both facades.
Forge keeps its recommended-version selection and advertising callback. NeoForge
does not open the Forge advertising page.

`ForgeInstaller` uses `IForgeVersionLoader` and `IForgeInstallerVersionMapper`.
Its existing constructors provide default implementations; the constructor taking
`MinecraftLauncher`, `IForgeVersionLoader`, and `IForgeInstallerVersionMapper`
allows callers to supply their own implementations.

Catalog, mapper, facade, and installer-profile fixture tests cover the integration.
Actual NeoForge processor execution still needs integration validation.

## Contributors

<a href="https://github.com/CmlLib/CmlLib.Core.Installer.Forge/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=CmlLib/CmlLib.Core.Installer.Forge" />
</a>

Made with [contrib.rocks](https://contrib.rocks).

Special thanks to [TaigoStudio](https://github.com/TaigoStudio) for contributing almost entire source codes.
