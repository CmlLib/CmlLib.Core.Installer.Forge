using System.Net;
using System.Text.Json;
using CmlLib.Core.Installer.Forge.Versions;

namespace CmlLib.Core.Installer.Forge.Tests;

public class NeoForgeVersionLoaderTests
{
    [Fact]
    public async Task LoadsVersionsForAllMinecraftReleasesInReverseCatalogOrder()
    {
        using var http = CreateClient(out _, "21.1.10", "20.2.88", "26.1.2.9-beta", "21.1.100", "26.1.0.19-beta");

        var versions = await new NeoForgeVersionLoader(http).GetNeoForgeVersions();

        Assert.Equal(new[]
        {
            new NeoForgeVersion("26.1", "26.1.0.19-beta"),
            new NeoForgeVersion("1.21.1", "21.1.100"),
            new NeoForgeVersion("26.1.2", "26.1.2.9-beta"),
            new NeoForgeVersion("1.20.2", "20.2.88"),
            new NeoForgeVersion("1.21.1", "21.1.10")
        }, versions);
    }

    [Theory]
    [InlineData("1.21.1", "21.1.10", "21.1.100", "21.1.9")]
    [InlineData("1.21.10", "21.10.11-beta", "21.10.2")]
    [InlineData("1.21.0", "21.0.167")]
    [InlineData("1.21", "21.0.167")]
    [InlineData("26.1", "26.1.0.19-beta", "26.1.0.9-beta")]
    [InlineData("26.1.2", "26.1.2.114", "26.1.2.9-beta")]
    public async Task FiltersExactMinecraftReleaseAndPreservesReverseCatalogOrder(string minecraft, params string[] expected)
    {
        using var http = CreateClient(out var handler,
            "21.1.9", "21.10.2", "21.1.100", "21.0.167", "21.1.10", "21.10.11-beta",
            "26.1.2.9-beta", "26.1.0.9-beta", "26.1.2.114", "26.1.0.19-beta");

        var versions = (await new NeoForgeVersionLoader(http).GetNeoForgeVersions(minecraft)).ToArray();

        Assert.Equal(expected, versions.Select(v => v.NeoForgeVersionName));
        Assert.All(versions, v => Assert.Equal(minecraft == "1.21.0" ? "1.21" : minecraft, v.MinecraftVersionName));
        Assert.Equal("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge", handler.Url);
    }

    [Fact]
    public async Task IncludesAlphaAndSnapshotsInReverseCatalogOrder()
    {
        using var http = CreateClient(out _, "21.1.100-beta", "21.1.100", "21.1.999-alpha",
            "21.1.101-beta.2", "21.1.101-beta.10", "26.1.0.0-alpha.1+snapshot-1");
        var versions = await new NeoForgeVersionLoader(http).GetNeoForgeVersions();
        Assert.Equal(new[] { "26.1.0.0-alpha.1+snapshot-1", "21.1.101-beta.10", "21.1.101-beta.2", "21.1.999-alpha", "21.1.100", "21.1.100-beta" },
            versions.Select(v => v.NeoForgeVersionName));
    }

    [Fact]
    public async Task PreservesArbitrarySuffixesAndMapsRecognizableMinecraftPrefixes()
    {
        var names = new[] { "21.1.100-rc.1+build.2", "26.1.2.114-preview", "21.1.100-beta.custom" };
        using var http = CreateClient(out _, names);
        var loader = new NeoForgeVersionLoader(http);

        var versions = await loader.GetNeoForgeVersions();

        Assert.Equal(names.Reverse(), versions.Select(v => v.NeoForgeVersionName));
        Assert.Contains(new NeoForgeVersion("26.1.2", "26.1.2.114-preview"), versions);
        Assert.Equal(new[] { "21.1.100-beta.custom", "21.1.100-rc.1+build.2" },
            (await loader.GetNeoForgeVersions("1.21.1")).Select(v => v.NeoForgeVersionName));
    }

    [Theory]
    [InlineData("custom-release")]
    [InlineData("99999999999999999999.1.2")]
    [InlineData("")]
    public async Task UnmappableCatalogEntryFailsBothFullAndFilteredQueries(string name)
    {
        using var http = CreateClient(out _, "21.1.100", name);
        var loader = new NeoForgeVersionLoader(http);

        var fullError = await Assert.ThrowsAsync<FormatException>(() => loader.GetNeoForgeVersions());
        var filteredError = await Assert.ThrowsAsync<FormatException>(() => loader.GetNeoForgeVersions("1.21.1"));

        Assert.Contains($"'{name}'", fullError.Message);
        Assert.Contains($"'{name}'", filteredError.Message);
    }

    [Fact]
    public async Task UnsupportedMinecraftVersionReturnsEmptyList()
    {
        using var http = CreateClient(out _, "21.1.100");
        Assert.Empty(await new NeoForgeVersionLoader(http).GetNeoForgeVersions("1.19.4"));
    }

    private static HttpClient CreateClient(out ManifestHandler handler, params string[] versions)
    {
        handler = new ManifestHandler(JsonSerializer.Serialize(new { versions }));
        return new HttpClient(handler);
    }

    private sealed class ManifestHandler : HttpMessageHandler
    {
        private readonly string _json;
        public ManifestHandler(string json) => _json = json;
        public string? Url { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json) });
        }
    }
}
