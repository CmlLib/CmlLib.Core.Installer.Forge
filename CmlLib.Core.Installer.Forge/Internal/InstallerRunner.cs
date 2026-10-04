using CmlLib.Core.Version;

namespace CmlLib.Core.Installer.Forge.Internal;

internal sealed class InstallerRunner
{
    private readonly MinecraftLauncher _launcher;

    public InstallerRunner(MinecraftLauncher launcher) => _launcher = launcher;

    public async Task<string> Install(
        string minecraftVersion,
        IForgeInstaller installer,
        ForgeInstallOptions options,
        Action? afterInstall = null)
    {
        var cancellationToken = options.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        if (options.SkipIfAlreadyInstalled && await IsVersionInstalled(installer.VersionName, cancellationToken))
            return installer.VersionName;

        var version = await _launcher.GetVersionAsync(minecraftVersion, cancellationToken);
        await _launcher.InstallAsync(version, options.FileProgress, options.ByteProgress, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(options.JavaPath))
            options = options.WithJavaPath(GetJavaPath(version));

        await installer.Install(_launcher.MinecraftPath, _launcher.GameInstaller, options);
        cancellationToken.ThrowIfCancellationRequested();
        afterInstall?.Invoke();
        await _launcher.GetAllVersionsAsync(cancellationToken);
        return installer.VersionName;
    }

    private async Task<bool> IsVersionInstalled(string versionName, CancellationToken cancellationToken)
    {
        try
        {
            await _launcher.GetVersionAsync(versionName, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private string GetJavaPath(IVersion version)
    {
        var javaPath = _launcher.GetJavaPath(version);
        if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
            javaPath = _launcher.GetDefaultJavaPath();
        if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
            throw new InvalidOperationException("Cannot find any java binary. Set java binary path");
        return javaPath;
    }
}
