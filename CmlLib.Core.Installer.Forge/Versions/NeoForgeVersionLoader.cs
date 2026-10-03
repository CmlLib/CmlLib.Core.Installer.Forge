using System.Text.Json;

namespace CmlLib.Core.Installer.Forge.Versions;

public class NeoForgeVersionLoader
{
    private readonly HttpClient _httpClient;

    public NeoForgeVersionLoader(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<NeoForgeVersion>> GetNeoForgeVersions()
    {
        using var stream = await _httpClient.GetStreamAsync(
            "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge");
        using var manifest = await JsonDocument.ParseAsync(stream);
        var versions = new List<NeoForgeVersion>();

        foreach (var item in manifest.RootElement.GetProperty("versions").EnumerateArray())
        {
            var name = item.GetString();
            if (name == null)
                continue;
            var version = NeoForgeVersionMapper.MapVersion(name);
            versions.Add(version);
        }

        versions.Reverse();
        return versions;
    }

    public async Task<IReadOnlyList<NeoForgeVersion>> GetNeoForgeVersions(string minecraftVersion)
    {
        var versions = await GetNeoForgeVersions();
        var mcVersion = NeoForgeVersionMapper.NormalizeMinecraftVersion(minecraftVersion);
        return versions.Where(version => version.MinecraftVersionName == mcVersion).ToArray();
    }
}
