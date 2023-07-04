using CmlLib.Core.Downloader;

namespace CmlLib.Core.Installer.Forge;

public class ForgeInstallOptions
{
    public ForgeInstallOptions(MinecraftPath path)
    {
        MinecraftPath = path;
    }

    public MinecraftPath MinecraftPath { get; set; }
    public IDownloader Downloader { get; set; } = new SequenceDownloader();
    public string? JavaPath { get; set; }
}