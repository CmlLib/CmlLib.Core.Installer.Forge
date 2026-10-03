using System.IO.Compression;
using System.Net;
using System.Text.Json;
using CmlLib.Core.FileExtractors;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installers;
using CmlLib.Core.VersionLoader;

namespace CmlLib.Core.Installer.Forge.Tests;

public sealed class NeoForgeInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExistingVersionSkipsVanillaAndInstallerDownload()
    {
        const string id = "neoforge-21.1.100";
        WriteVersion(id);
        var calls = new List<string>();
        using var http = CreateClient();
        var installer = new NeoForgeInstaller(CreateLauncher(calls), http);

        var result = await installer.Install(new NeoForgeVersion("1.21.1", "21.1.100"), new ForgeInstallOptions());

        Assert.Equal(id, result);
        Assert.Empty(calls);
    }

    [Theory]
    [InlineData("1.21.1", "1.21.1", "21.1.100", false)]
    [InlineData("1.21.0", "1.21", "21.0.167", false)]
    [InlineData("26.1.0", "26.1", "26.1.0.19-beta", false)]
    [InlineData("1.21.1", "1.21.1", "21.1.100", true)]
    public async Task InstallsNormalizedVanillaThenNeoForgeAndRefreshesVersionList(
        string minecraft, string normalizedMinecraft, string neoForge, bool alreadyInstalled)
    {
        var id = $"neoforge-{neoForge}";
        WriteVersion(normalizedMinecraft);
        if (alreadyInstalled)
            WriteVersion(id);
        var calls = new List<string>();
        var launcher = CreateLauncher(calls, id);
        using var http = CreateClient();
        var installer = new NeoForgeInstaller(launcher, http);
        var options = new ForgeInstallOptions { JavaPath = "chosen-java", SkipIfAlreadyInstalled = false };

        var result = await installer.Install(new NeoForgeVersion(minecraft, neoForge), options);

        Assert.Equal(id, result);
        // Vanilla files are processed before the installer; profile libraries follow it.
        Assert.Equal(new[] { "files", "loader", "files" }, calls);
        Assert.True(launcher.Versions!.TryGetVersionMetadata(id, out _));
        Assert.Equal("chosen-java", options.JavaPath);
        Assert.True(File.Exists(new MinecraftPath(_root).GetVersionJsonPath(id)));
    }

    [Theory]
    [InlineData(null, "neoforge-21.1.9")]
    [InlineData("21.1.100", "neoforge-21.1.100")]
    public async Task SelectsFirstInReverseServerOrderOrExactNeoForgeVersion(string? requested, string expected)
    {
        WriteVersion("neoforge-21.1.9");
        WriteVersion("neoforge-21.1.100");
        var calls = new List<string>();
        // The last server item wins even when it is numerically smaller.
        using var http = CreateClient("21.1.100", "21.1.9");
        var installer = new NeoForgeInstaller(CreateLauncher(calls), http);

        string result;
        if (requested == null)
            result = await installer.Install("1.21.1");
        else
            result = await installer.Install("1.21.1", requested);

        Assert.Equal(expected, result);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task MissingMinecraftReleaseFailsBeforeDownloading()
    {
        var calls = new List<string>();
        using var http = CreateClient("21.1.100");
        var installer = new NeoForgeInstaller(CreateLauncher(calls), http);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => installer.Install("1.21.2"));

        Assert.Contains("1.21.2", error.Message);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task ExactVersionFromAnotherMinecraftReleaseFailsBeforeDownloading()
    {
        var calls = new List<string>();
        using var http = CreateClient("21.1.100", "21.2.1");
        var installer = new NeoForgeInstaller(CreateLauncher(calls), http);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => installer.Install("1.21.1", "21.2.1"));

        Assert.Contains("21.2.1", error.Message);
        Assert.Empty(calls);
    }

    private MinecraftLauncher CreateLauncher(List<string> calls, string versionId = "unused")
    {
        var path = new MinecraftPath(_root);
        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.VersionLoader = new LocalJsonVersionLoader(path);
        parameters.FileExtractors = new FileExtractorCollection();
        parameters.GameInstaller = new FixtureInstaller(calls, versionId);
        return new MinecraftLauncher(parameters);
    }

    private static HttpClient CreateClient(params string[] versions) => new(new ManifestHandler(versions));

    private sealed class ManifestHandler : HttpMessageHandler
    {
        private readonly string[] _versions;
        public ManifestHandler(string[] versions) => _versions = versions;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { versions = _versions }))
            });
    }

    private void WriteVersion(string id)
    {
        var path = new MinecraftPath(_root).GetVersionJsonPath(id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, VersionJson(id));
    }

    private static string VersionJson(string id) =>
        JsonSerializer.Serialize(new { id, type = "release", libraries = Array.Empty<object>() });

    private sealed class FixtureInstaller : IGameInstaller
    {
        private readonly List<string> _calls;
        private readonly string _versionId;
        public FixtureInstaller(List<string> calls, string versionId)
        {
            _calls = calls;
            _versionId = versionId;
        }

        public ValueTask Install(IEnumerable<GameFile> files, IProgress<InstallerProgressChangedEventArgs>? fileProgress,
            IProgress<ByteProgress>? byteProgress, CancellationToken cancellationToken)
        {
            var installerFile = files.SingleOrDefault(file => file.Url?.EndsWith("-installer.jar", StringComparison.Ordinal) == true);
            if (installerFile == null)
            {
                _calls.Add("files");
                return ValueTask.CompletedTask;
            }

            _calls.Add("loader");
            Directory.CreateDirectory(Path.GetDirectoryName(installerFile.Path!)!);
            using var zip = ZipFile.Open(installerFile.Path!, ZipArchiveMode.Create);
            using (var writer = new StreamWriter(zip.CreateEntry("install_profile.json").Open()))
                writer.Write("{\"libraries\":[]}");
            using (var writer = new StreamWriter(zip.CreateEntry("version.json").Open()))
                writer.Write(VersionJson(_versionId));
            return ValueTask.CompletedTask;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
