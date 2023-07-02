namespace CmlLib.Core.Installer.Forge.Versions;

public interface IForgeVersionNameResolver
{
    string Resolve(string mcVersion, string forgeVersion);
}
