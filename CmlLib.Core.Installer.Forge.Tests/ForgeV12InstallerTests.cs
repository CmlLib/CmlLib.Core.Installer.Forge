using System.IO.Compression;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Installers;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;

namespace CmlLib.Core.Installer.Forge.Tests;

public sealed class ForgeV12InstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ForgeMapperConvertsVersionAndCopiesEmbeddedMavenAndVersionJar()
    {
        var version = new ForgeVersion("1.20.1", "47.1.0")
        {
            Files = new[] { new ForgeVersionFile { Type = "installer", DirectUrl = "https://example.com/forge.jar" } }
        };
        var path = new MinecraftPath(_root);
        var downloader = new FixtureInstaller("maven/net/minecraftforge/forge/1.20.1-47.1.0/forge-1.20.1-47.1.0.jar");
        var installer = Assert.IsType<ForgeV12Installer>(new ForgeInstallerVersionMapper().CreateInstaller(version));
        // The mapped artifact no longer depends on mutable Forge version metadata.
        version.Files = new[] { new ForgeVersionFile { Type = "installer", DirectUrl = "https://example.com/updated-forge.jar" } };

        await installer.Install(path, downloader, new ForgeInstallOptions { JavaPath = "unused-java" });

        Assert.Equal("1.20.1", installer.VersionArtifact.MinecraftVersion);
        Assert.Equal("47.1.0", installer.VersionArtifact.LoaderVersion);
        Assert.Equal("1.20.1-forge-47.1.0", installer.VersionName);
        Assert.Equal("maven/net/minecraftforge/forge/1.20.1-47.1.0/forge-1.20.1-47.1.0.jar", installer.VersionArtifact.EmbeddedVersionJar);
        Assert.Equal("https://example.com/forge.jar", downloader.Download!.Url);
        Assert.Equal("fixture-json", File.ReadAllText(path.GetVersionJsonPath(installer.VersionName)));
        Assert.Equal("fixture-jar", File.ReadAllText(path.GetVersionJarPath(installer.VersionName)));
        Assert.True(File.Exists(Path.Combine(path.Library, "net/minecraftforge/forge/1.20.1-47.1.0/forge-1.20.1-47.1.0.jar")));
    }

    [Fact]
    public async Task NeoForgeArtifactUsesForgeV12InstallerWithItsOwnVersionId()
    {
        var path = new MinecraftPath(_root);
        var downloader = new FixtureInstaller("maven/net/neoforged/neoforge/26.1.0.19-beta/neoforge-26.1.0.19-beta.jar");
        var artifact = new ForgeV12VersionArtifact("26.1", "26.1.0.19-beta", "neoforge-26.1.0.19-beta",
            "https://maven.neoforged.net/releases/net/neoforged/neoforge/26.1.0.19-beta/neoforge-26.1.0.19-beta-installer.jar", null);
        IForgeInstaller installer = new ForgeV12Installer(artifact);

        await installer.Install(path, downloader, new ForgeInstallOptions { JavaPath = "unused-java" });

        var v12Installer = Assert.IsType<ForgeV12Installer>(installer);
        Assert.Equal("26.1", v12Installer.VersionArtifact.MinecraftVersion);
        Assert.Equal("26.1.0.19-beta", v12Installer.VersionArtifact.LoaderVersion);
        Assert.Equal("https://maven.neoforged.net/releases/net/neoforged/neoforge/26.1.0.19-beta/neoforge-26.1.0.19-beta-installer.jar", v12Installer.VersionArtifact.InstallerUrl);
        Assert.Null(v12Installer.VersionArtifact.EmbeddedVersionJar);
        Assert.Equal("neoforge-26.1.0.19-beta", installer.VersionName);
        Assert.Equal(v12Installer.VersionArtifact.InstallerUrl, downloader.Download!.Url);
        Assert.Equal("fixture-json", File.ReadAllText(path.GetVersionJsonPath(installer.VersionName)));
        Assert.True(File.Exists(Path.Combine(path.Library, "net/neoforged/neoforge/26.1.0.19-beta/neoforge-26.1.0.19-beta.jar")));
        Assert.False(File.Exists(path.GetVersionJarPath(installer.VersionName)));
    }

    private sealed class FixtureInstaller : IGameInstaller
    {
        private readonly string _embeddedJar;
        public FixtureInstaller(string embeddedJar) => _embeddedJar = embeddedJar;
        public GameFile? Download { get; private set; }

        public ValueTask Install(IEnumerable<GameFile> files, IProgress<InstallerProgressChangedEventArgs>? fileProgress,
            IProgress<ByteProgress>? byteProgress, CancellationToken cancellationToken)
        {
            foreach (var file in files)
            {
                Download = file;
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path!)!);
                using var zip = ZipFile.Open(file.Path!, ZipArchiveMode.Create);
                Write(zip, "install_profile.json", "{\"libraries\":[]}");
                Write(zip, "version.json", "fixture-json");
                Write(zip, _embeddedJar, "fixture-jar");
            }
            return ValueTask.CompletedTask;
        }

        private static void Write(ZipArchive zip, string path, string text)
        {
            using var writer = new StreamWriter(zip.CreateEntry(path).Open());
            writer.Write(text);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
