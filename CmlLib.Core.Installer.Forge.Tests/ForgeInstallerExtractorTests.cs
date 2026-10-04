using System.IO.Compression;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge.Installers;
using CmlLib.Core.Installers;

namespace CmlLib.Core.Installer.Forge.Tests;

public sealed class ForgeInstallerExtractorTests
{
    private static readonly ForgeV12VersionArtifact Artifact = new(
        "1.21.1", "21.1.100", "fixture", "https://example.com/installer.jar", null);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeToleratesMissingOrReplacedDirectory(bool replaceWithFile)
    {
        var downloader = new FixtureInstaller();
        using var extractor = await ForgeInstallerExtractor.DownloadAndExtractInstaller(
            Artifact, downloader, new ForgeInstallOptions());
        Directory.Delete(extractor.ExtractedDir, true);
        if (replaceWithFile)
            File.WriteAllText(extractor.ExtractedDir, "an unexpected file occupies the directory path");

        try
        {
            extractor.Dispose();
            extractor.Dispose();
        }
        finally
        {
            if (replaceWithFile)
                File.Delete(extractor.ExtractedDir);
        }
    }

    [Fact]
    public async Task CanceledDownloadCleansPartialFilesAndPreservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var downloader = new FixtureInstaller((file, token) =>
        {
            File.WriteAllText(file.Path!, "partial download");
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        });

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ForgeInstallerExtractor.DownloadAndExtractInstaller(Artifact, downloader,
                new ForgeInstallOptions { CancellationToken = cancellation.Token }));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.False(Directory.Exists(Path.GetDirectoryName(downloader.DownloadPath)));
    }

    [Fact]
    public async Task InvalidArchiveCleansDownloadDirectory()
    {
        var downloader = new FixtureInstaller((file, _) => File.WriteAllText(file.Path!, "not a zip"));

        await Assert.ThrowsAnyAsync<Exception>(() => ForgeInstallerExtractor.DownloadAndExtractInstaller(
            Artifact, downloader, new ForgeInstallOptions()));

        Assert.False(Directory.Exists(Path.GetDirectoryName(downloader.DownloadPath)));
    }

    private sealed class FixtureInstaller : IGameInstaller
    {
        private readonly Action<GameFile, CancellationToken>? _download;
        public FixtureInstaller(Action<GameFile, CancellationToken>? download = null) => _download = download;
        public string? DownloadPath { get; private set; }
        public ValueTask Install(IEnumerable<GameFile> files, IProgress<InstallerProgressChangedEventArgs>? fileProgress,
            IProgress<ByteProgress>? byteProgress, CancellationToken cancellationToken)
        {
            foreach (var file in files)
            {
                DownloadPath = file.Path;
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path!)!);
                if (_download != null)
                    _download(file, cancellationToken);
                else
                {
                    using var zip = ZipFile.Open(file.Path!, ZipArchiveMode.Create);
                    using var writer = new StreamWriter(zip.CreateEntry("install_profile.json").Open());
                    writer.Write("{}");
                }
            }
            return ValueTask.CompletedTask;
        }
    }
}
