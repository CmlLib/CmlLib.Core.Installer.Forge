using System.Text.Json.Serialization;
using CmlLib.Core.Installer.Forge.Models;
using HtmlAgilityPack;

namespace CmlLib.Core.Installer.Forge.Versions;

public class NeoForgeVersionLoader
{
    private readonly HttpClient _httpClient;

    private const string _forgeVersionManifest =
        "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge";
    
    public NeoForgeVersionLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<NeoForgeVersion>> GetNeoForgeVersions(string mcVersion)
    {
        var manifestVersion = mcVersion.Substring(2);
        
        var stream = await _httpClient.GetStreamAsync(_forgeVersionManifest);

        var manifest = await System.Text.Json.JsonSerializer.DeserializeAsync<NeoForgeManifest>(stream);
        
        if (manifest == null)
            return Array.Empty<NeoForgeVersion>();

        var currentGameVersions = manifest.Versions.Where(c => c.StartsWith(manifestVersion));
        
        return currentGameVersions.Select(version => new NeoForgeVersion(mcVersion, version));
    }
    
    private IEnumerable<NeoForgeVersionFile> getForgeVersionFiles(HtmlNode node)
    {
        var lis = node.SelectNodes("ul[1]/li");
        if (lis == null)
            return Enumerable.Empty<NeoForgeVersionFile>();

        var files = new List<NeoForgeVersionFile>();
        foreach (var li in lis)
        {
            var forgeVersionFile = new NeoForgeVersionFile();
            string? firstLink = null, secondLink = null;

            var firstANode = li.SelectSingleNode("a[1]");
            if (firstANode != null)
            {
                firstLink = firstANode.GetAttributeValue("href", "").Trim();
                forgeVersionFile.Type = firstANode.InnerText.Trim();
            }

            var infoTooltip = li.Descendants().FirstOrDefault(node => node.HasClass("info-tooltip"));
            if (infoTooltip != default)
            {
                forgeVersionFile.MD5 = infoTooltip.ChildNodes[2].InnerText.Trim();
                forgeVersionFile.SHA1 = infoTooltip.ChildNodes[6].InnerText.Trim();
                secondLink = infoTooltip
                    .Descendants("a")
                    .FirstOrDefault()?
                    .GetAttributeValue("href", "")?
                    .Trim();
            }

            if (string.IsNullOrEmpty(secondLink))
            {
                forgeVersionFile.DirectUrl = firstLink;
            }
            else
            {
                forgeVersionFile.AdUrl = firstLink;
                forgeVersionFile.DirectUrl = secondLink;
            }

            files.Add(forgeVersionFile);
        }

        return files;
    }
}
