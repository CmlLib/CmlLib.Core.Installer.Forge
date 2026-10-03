using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;
using CmlLib.Core.VersionLoader;

namespace CmlLib.Core.Installer.Forge.Tests;

public sealed class ForgeInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task UsesInjectedVersionLoaderForDiscovery()
    {
        var versions = new[] { new ForgeVersion("1.21.1", "52.0.1") };
        var loader = new StubLoader(versions);
        var installer = new ForgeInstaller(CreateLauncher(), loader, new StubMapper());

        var result = await installer.GetForgeVersions("1.21.1");

        Assert.Same(versions, result);
        Assert.Equal("1.21.1", loader.RequestedMinecraftVersion);
    }

    [Theory]
    [InlineData(true, true, "recommended")]
    [InlineData(false, true, "latest")]
    [InlineData(false, false, "first")]
    public async Task UsesInjectedMapperAndPreservesForgeSelectionPriority(bool recommended, bool latest, string expected)
    {
        var versions = new[]
        {
            new ForgeVersion("1.21.1", "first"),
            new ForgeVersion("1.21.1", "latest") { IsLatestVersion = latest },
            new ForgeVersion("1.21.1", "recommended") { IsRecommendedVersion = recommended }
        };
        WriteVersion(expected);
        var loader = new StubLoader(versions);
        var mapper = new StubMapper();
        var installer = new ForgeInstaller(CreateLauncher(), loader, mapper);

        var result = await installer.Install("1.21.1");

        Assert.Equal(expected, result);
        Assert.Same(versions.Single(v => v.ForgeVersionName == expected), mapper.SelectedVersion);
        Assert.Equal("1.21.1", loader.RequestedMinecraftVersion);
    }

    [Fact]
    public async Task ExactBuildSelectionUsesInjectedDependencies()
    {
        var selected = new ForgeVersion("1.21.1", "exact");
        WriteVersion("exact");
        var loader = new StubLoader(new[] { new ForgeVersion("1.21.1", "other"), selected });
        var mapper = new StubMapper();
        var installer = new ForgeInstaller(CreateLauncher(), loader, mapper);

        Assert.Equal("exact", await installer.Install("1.21.1", "exact"));
        Assert.Same(selected, mapper.SelectedVersion);
    }

    private MinecraftLauncher CreateLauncher()
    {
        var path = new MinecraftPath(_root);
        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.VersionLoader = new LocalJsonVersionLoader(path);
        return new MinecraftLauncher(parameters);
    }

    private void WriteVersion(string id)
    {
        var path = new MinecraftPath(_root).GetVersionJsonPath(id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"{{\"id\":\"{id}\",\"type\":\"release\",\"libraries\":[]}}");
    }

    private sealed class StubLoader : IForgeVersionLoader
    {
        private readonly IEnumerable<ForgeVersion> _versions;
        public StubLoader(IEnumerable<ForgeVersion> versions) => _versions = versions;
        public string? RequestedMinecraftVersion { get; private set; }
        public Task<IEnumerable<ForgeVersion>> GetForgeVersions(string minecraftVersion)
        {
            RequestedMinecraftVersion = minecraftVersion;
            return Task.FromResult(_versions);
        }
    }

    private sealed class StubMapper : IForgeInstallerVersionMapper
    {
        public ForgeVersion? SelectedVersion { get; private set; }
        public IForgeInstaller CreateInstaller(ForgeVersion version)
        {
            SelectedVersion = version;
            return new StubInstaller(version.ForgeVersionName);
        }
    }

    private sealed class StubInstaller : IForgeInstaller
    {
        public StubInstaller(string versionName) => VersionName = versionName;
        public string VersionName { get; }
        public Task Install(MinecraftPath path, IGameInstaller installer, ForgeInstallOptions options) =>
            throw new InvalidOperationException("Already installed versions must be skipped.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
