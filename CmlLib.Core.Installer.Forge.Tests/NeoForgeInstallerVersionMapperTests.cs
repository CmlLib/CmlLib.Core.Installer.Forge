using CmlLib.Core.Installer.Forge.Versions;

namespace CmlLib.Core.Installer.Forge.Tests;

public class NeoForgeInstallerVersionMapperTests
{
    [Theory]
    [InlineData("1.20.2", "20.2.88", "1.20.2")]
    [InlineData("1.21.0", "21.0.167", "1.21")]
    [InlineData("26.1.0", "26.1.0.19-beta", "26.1")]
    [InlineData("26.1.2", "26.1.2.114-preview+build.2", "26.1.2")]
    public void CreatesNeoForgeArtifactWithoutChangingTheVersionModel(string minecraft, string loader, string normalized)
    {
        var version = new NeoForgeVersion(minecraft, loader);
        var installer = new NeoForgeInstallerVersionMapper().CreateInstaller(version);
        var artifact = new NeoForgeInstallerVersionMapper().CreateVersionArtifact(version);
        Assert.Equal(artifact, installer.VersionArtifact);

        Assert.Equal(normalized, artifact.MinecraftVersion);
        Assert.Equal(loader, artifact.LoaderVersion);
        Assert.Equal($"neoforge-{loader}", artifact.VersionName);
        Assert.Equal(artifact.VersionName, installer.VersionName);
        Assert.Equal($"https://maven.neoforged.net/releases/net/neoforged/neoforge/{loader}/neoforge-{loader}-installer.jar", artifact.InstallerUrl);
        Assert.Null(artifact.EmbeddedVersionJar);
        Assert.Equal(new NeoForgeVersion(minecraft, loader), version);
    }
}
