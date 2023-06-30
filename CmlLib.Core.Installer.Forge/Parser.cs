using HtmlAgilityPack;
using System.Security.Cryptography;

namespace CmlLib.Core.Installer.Forge
{
    public class Parser
    {
        private static HttpClient httpClient = new HttpClient();

        public static async Task<string> getForgeUrl(string mcVersion, string forgeVersion)
        {
            var document = new HtmlDocument();
            var html = await httpClient.GetStringAsync($"https://files.minecraftforge.net/net/minecraftforge/forge/index_{mcVersion}.html");
            document.LoadHtml(html);
            var rows = document.DocumentNode.SelectNodes("//html[1]//body[1]//main[1]//div[2]//div[2]//div[2]//table[1]//tbody[1]//tr").ToList();
            foreach (var row in rows)
            {
                var current_version = ClearName(row.Descendants(0).Where(n => n.HasClass("download-version")).FirstOrDefault().FirstChild.OuterHtml.Replace(" ", ""));
                if (current_version == forgeVersion)
                    return GetQueryString(row.ChildNodes[5].ChildNodes[1].ChildNodes[3].ChildNodes[3].Attributes["href"].Value, "url=");
            }
            throw new Exception("The version was not found on the official website");
        }

        public static string getAdUrl() =>
            $"https://adfoc.us/serve/sitelinks/?id=271228&url=https://maven.minecraftforge.net/";

        private static string ClearName(string name) => name.Replace(" ", "").Replace("\n", "");

        private static string GetQueryString(string url, string key)
        {
            int index = url.IndexOf('?');
            var query = url.Substring(index + 1).Split('&').SingleOrDefault(s => s.StartsWith(key));
            return query == null ? url : query.Replace(key, null);
        }

        public static async Task DownloadFile(string mcVersion, string forgeVersion, string install_folder)
        {
            Directory.CreateDirectory(install_folder);
            var fileUrl = await getForgeUrl(mcVersion, forgeVersion);
            var httpResult = await httpClient.GetAsync(fileUrl);
            using var resultStream = await httpResult.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(Path.Combine(install_folder, "installer.jar"));
            resultStream.CopyTo(fileStream);
        }

        public static string CalculateMD5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
