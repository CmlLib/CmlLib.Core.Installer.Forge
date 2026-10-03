namespace CmlLib.Core.Installer.Forge.Versions;

public interface IForgeVersionLoader
{
    Task<IEnumerable<ForgeVersion>> GetForgeVersions(string minecraftVersion);
}
