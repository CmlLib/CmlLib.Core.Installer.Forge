#pragma warning disable CS8601
using CmlLib.Core.Downloader;


namespace CmlLib.Core.Installer.Forge;

public class MForge
{
    private readonly MinecraftPath minecraftPath;
    private string JavaPath;
    private CMLauncher launcher;
    private readonly IDownloader downloader;
    public event DownloadFileChangedHandler? FileChanged;
    public event EventHandler<string>? InstallerOutput;

    public MForge(MinecraftPath mc, CMLauncher launcher)
    {
        minecraftPath = mc;
        this.launcher = launcher;
        downloader = new SequenceDownloader();
    }

    public async Task<string> Install(string mcVersion, string forgeVersion, bool AlwaysUpdate = false)
    {
        JavaPath = launcher.GetJavaPath(await launcher.GetVersionAsync(mcVersion));

        if (Func.IsOldType(mcVersion))
        {
            var legacy = new FLegacy(minecraftPath, JavaPath, launcher, downloader, FileChanged, InstallerOutput);
            return await legacy.Install(mcVersion, forgeVersion, AlwaysUpdate);
        }

        var newest = new FNewest(minecraftPath, JavaPath, launcher, downloader, FileChanged, InstallerOutput);
        return await newest.Install(mcVersion, forgeVersion, AlwaysUpdate);
    }
  
    private void fireEvent(MFile kind, string name, int total, int progressed)
    {
        FileChanged?.Invoke(new DownloadFileChangedEventArgs(kind, this, name, total, progressed));
    }
}
