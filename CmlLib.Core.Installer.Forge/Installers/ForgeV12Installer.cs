using CmlLib.Core.Installers;
using CmlLib.Utils;
using System.Text.Json;

namespace CmlLib.Core.Installer.Forge.Installers;

/* 1.12 ~ */
public class ForgeV12Installer : IForgeInstaller
{
    public ForgeV12Installer(ForgeV12VersionArtifact artifact)
    {
        VersionArtifact = artifact;
    }

    public string VersionName => VersionArtifact.VersionName;
    public ForgeV12VersionArtifact VersionArtifact { get; }

    public async Task Install(MinecraftPath path, IGameInstaller installer, ForgeInstallOptions options)
    {
        var cancellationToken = options.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(options.JavaPath))
            throw new ArgumentNullException(nameof(options.JavaPath));
        var artifact = VersionArtifact;
        var processor = new ForgeInstallProcessor(options.JavaPath);

        using var extractor = await ForgeInstallerExtractor.DownloadAndExtractInstaller(artifact, installer, options);
        using var installerProfileStream = extractor.OpenInstallerProfile();
        using var installerProfile = await JsonDocument.ParseAsync(installerProfileStream, cancellationToken: cancellationToken);

        await extractMavens(extractor.ExtractedDir, path, cancellationToken);
        await installLibraries(installerProfile.RootElement, path, installer, options);
        await processor.MapAndStartProcessors(
            extractor.ExtractedDir,
            path.GetVersionJarPath(artifact.MinecraftVersion),
            path.Library,
            installerProfile.RootElement,
            options.FileProgress,
            options.InstallerOutput,
            cancellationToken);
        await copyVersionFiles(extractor.ExtractedDir, path, artifact, cancellationToken);
    }

    private async Task extractMavens(string installerPath, MinecraftPath minecraftPath, CancellationToken cancellationToken)
    {
        var org = Path.Combine(installerPath, "maven");
        if (Directory.Exists(org))
            await IOUtil.CopyDirectory(org, minecraftPath.Library, cancellationToken);
    }

    private async Task installLibraries(
        JsonElement installerProfile,
        MinecraftPath path,
        IGameInstaller installer,
        ForgeInstallOptions options)
    {
        if (installerProfile.TryGetProperty("libraries", out var libraryProp) &&
            libraryProp.ValueKind == JsonValueKind.Array)
        {
            var libraryInstaller = new ForgeLibraryInstaller(installer, options.RulesEvaluator, options.RulesContext, MojangServer.Library);
            await libraryInstaller.Install(
                path,
                libraryProp,
                options.FileProgress,
                options.ByteProgress,
                options.CancellationToken);
        }
    }

    private async Task copyVersionFiles(
        string installerDir, MinecraftPath minecraftPath, ForgeV12VersionArtifact artifact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrEmpty(artifact.EmbeddedVersionJar))
        {
            var jar = Path.Combine(installerDir, artifact.EmbeddedVersionJar);
            if (File.Exists(jar)) //fix 1.17+
            {
                var jarPath = minecraftPath.GetVersionJarPath(VersionName);
                IOUtil.CreateDirectoryForFile(jarPath);
                await IOUtil.CopyFileAsync(jar, jarPath, cancellationToken);
            }
        }

        // Publish version metadata only after all install work has completed.
        var versionJsonSource = Path.Combine(installerDir, "version.json");
        var versionJsonDest = minecraftPath.GetVersionJsonPath(VersionName);
        IOUtil.CreateDirectoryForFile(versionJsonDest);
        await IOUtil.CopyFileAtomicAsync(versionJsonSource, versionJsonDest, cancellationToken);
    }
}
