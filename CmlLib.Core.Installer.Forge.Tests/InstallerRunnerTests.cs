using CmlLib.Core.FileExtractors;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Internal;
using CmlLib.Core.Installers;
using CmlLib.Core.VersionLoader;

namespace CmlLib.Core.Installer.Forge.Tests;

public sealed class InstallerRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CallbackRunsOnlyAfterSuccessfulLoaderInstallation(bool fail)
    {
        WriteVersion("1.21.1");
        var calls = new List<string>();
        var path = new MinecraftPath(_root);
        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.VersionLoader = new LocalJsonVersionLoader(path);
        parameters.FileExtractors = new FileExtractorCollection();
        parameters.GameInstaller = new RecordingInstaller(calls);
        var launcher = new MinecraftLauncher(parameters);
        var options = new ForgeInstallOptions { JavaPath = "chosen-java" };
        var installer = new StubInstaller(options, () =>
        {
            calls.Add("loader");
            if (fail)
                throw new InvalidOperationException("fixture failure");
            WriteVersion("fixture-loader");
        });
        var task = new InstallerRunner(launcher).Install("1.21.1", installer, options, () => calls.Add("callback"));

        if (fail)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => task);
            Assert.Equal(new[] { "vanilla", "loader" }, calls);
        }
        else
        {
            Assert.Equal("fixture-loader", await task);
            Assert.Equal(new[] { "vanilla", "loader", "callback" }, calls);
            Assert.True(launcher.Versions!.TryGetVersionMetadata("fixture-loader", out _));
        }
    }

    private void WriteVersion(string id)
    {
        var path = new MinecraftPath(_root).GetVersionJsonPath(id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"{{\"id\":\"{id}\",\"type\":\"release\",\"libraries\":[]}}");
    }

    private sealed class StubInstaller : IForgeInstaller
    {
        private readonly ForgeInstallOptions _options;
        private readonly Action _install;
        public StubInstaller(ForgeInstallOptions options, Action install)
        {
            _options = options;
            _install = install;
        }
        public string VersionName => "fixture-loader";
        public Task Install(MinecraftPath path, IGameInstaller installer, ForgeInstallOptions options)
        {
            Assert.Same(_options, options);
            _install();
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingInstaller : IGameInstaller
    {
        private readonly List<string> _calls;
        public RecordingInstaller(List<string> calls) => _calls = calls;
        public ValueTask Install(IEnumerable<GameFile> files, IProgress<InstallerProgressChangedEventArgs>? fileProgress,
            IProgress<ByteProgress>? byteProgress, CancellationToken cancellationToken)
        {
            _calls.Add("vanilla");
            return ValueTask.CompletedTask;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
