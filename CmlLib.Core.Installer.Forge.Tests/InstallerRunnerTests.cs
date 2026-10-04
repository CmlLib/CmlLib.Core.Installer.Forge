using CmlLib.Core.FileExtractors;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Internal;
using CmlLib.Core.Installers;
using CmlLib.Core.Java;
using CmlLib.Core.Rules;
using CmlLib.Core.VersionLoader;
using System.Text.Json;

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

    [Fact]
    public async Task ReusedOptionsResolveJavaForEachVersionWithoutMutatingCaller()
    {
        WriteVersion("first", "java-first");
        WriteVersion("second", "java-second");
        var path = new MinecraftPath(_root);
        var java = new RecordingJavaResolver(_root);
        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.VersionLoader = new LocalJsonVersionLoader(path);
        parameters.FileExtractors = new FileExtractorCollection();
        parameters.GameInstaller = new RecordingInstaller(new List<string>());
        parameters.JavaPathResolver = java;
        var runner = new InstallerRunner(new MinecraftLauncher(parameters));
        using var cancellation = new CancellationTokenSource();
        var options = new ForgeInstallOptions
        {
            SkipIfAlreadyInstalled = false,
            CancellationToken = cancellation.Token,
            FileProgress = new Progress<InstallerProgressChangedEventArgs>(),
            ByteProgress = new Progress<ByteProgress>(),
            InstallerOutput = new Progress<string>()
        };
        var selected = new List<string?>();
        var installer = new InspectingInstaller(actual =>
        {
            Assert.NotSame(options, actual);
            Assert.Same(options.RulesEvaluator, actual.RulesEvaluator);
            Assert.Same(options.RulesContext, actual.RulesContext);
            Assert.Same(options.FileProgress, actual.FileProgress);
            Assert.Same(options.ByteProgress, actual.ByteProgress);
            Assert.Same(options.InstallerOutput, actual.InstallerOutput);
            Assert.Equal(options.CancellationToken, actual.CancellationToken);
            Assert.False(actual.SkipIfAlreadyInstalled);
            selected.Add(Path.GetFileName(actual.JavaPath));
        });

        await runner.Install("first", installer, options);
        await runner.Install("second", installer, options);

        Assert.Equal(new[] { "java-first", "java-second" }, java.Requests);
        Assert.Equal(new[] { "java-first", "java-second" }, selected);
        Assert.Null(options.JavaPath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationStopsBeforeLoaderAndCallback(bool alreadyCanceled)
    {
        WriteVersion("1.21.1");
        using var cancellation = new CancellationTokenSource();
        if (alreadyCanceled)
            cancellation.Cancel();
        var calls = new List<string>();
        var path = new MinecraftPath(_root);
        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.VersionLoader = new LocalJsonVersionLoader(path);
        parameters.FileExtractors = new FileExtractorCollection();
        parameters.GameInstaller = new RecordingInstaller(calls, token =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        });
        var runner = new InstallerRunner(new MinecraftLauncher(parameters));
        var installer = new InspectingInstaller(_ => calls.Add("loader"));

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.Install(
            "1.21.1", installer, new ForgeInstallOptions { JavaPath = "unused", CancellationToken = cancellation.Token },
            () => calls.Add("callback")));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(alreadyCanceled ? Array.Empty<string>() : new[] { "vanilla" }, calls);
    }

    private void WriteVersion(string id, string? javaComponent = null)
    {
        var path = new MinecraftPath(_root).GetVersionJsonPath(id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            id, type = "release", libraries = Array.Empty<object>(),
            javaVersion = javaComponent == null ? null : new { component = javaComponent, majorVersion = 17 }
        }));
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
        private readonly Action<CancellationToken>? _onInstall;
        public RecordingInstaller(List<string> calls, Action<CancellationToken>? onInstall = null)
        {
            _calls = calls;
            _onInstall = onInstall;
        }
        public ValueTask Install(IEnumerable<GameFile> files, IProgress<InstallerProgressChangedEventArgs>? fileProgress,
            IProgress<ByteProgress>? byteProgress, CancellationToken cancellationToken)
        {
            _calls.Add("vanilla");
            _onInstall?.Invoke(cancellationToken);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InspectingInstaller : IForgeInstaller
    {
        private readonly Action<ForgeInstallOptions> _install;
        public InspectingInstaller(Action<ForgeInstallOptions> install) => _install = install;
        public string VersionName => "fixture-loader";
        public Task Install(MinecraftPath path, IGameInstaller installer, ForgeInstallOptions options)
        {
            _install(options);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJavaResolver : IJavaPathResolver
    {
        private readonly string _root;
        public RecordingJavaResolver(string root) => _root = root;
        public List<string> Requests { get; } = new();
        public string GetJavaBinaryPath(JavaVersion version, RulesEvaluatorContext rules)
        {
            Requests.Add(version.Component);
            var path = Path.Combine(_root, version.Component);
            File.WriteAllText(path, "fixture");
            return path;
        }
        public string GetJavaDirPath(JavaVersion version, RulesEvaluatorContext rules) => _root;
        public string? GetDefaultJavaBinaryPath(RulesEvaluatorContext rules) => null;
        public IReadOnlyCollection<string> GetInstalledJavaVersions() => Array.Empty<string>();
        public IReadOnlyCollection<string> GetInstalledJavaVersions(RulesEvaluatorContext rules) => Array.Empty<string>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
