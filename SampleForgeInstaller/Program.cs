using CmlLib.Core.Installer.Forge;

var httpClient = new HttpClient();
var forgeVersionLoader = new ForgeVersionLoader(httpClient);
var result = await forgeVersionLoader.GetForgeVersions("1.12.2");
foreach (var v in result)
{
    Console.WriteLine("MinecraftVersionName : " + v.MinecraftVersionName);
    Console.WriteLine("ForgeVersionName : " + v.ForgeVersionName);
    Console.WriteLine("Time : " + v.Time);
    if (v.Files != null)
    {
        foreach (var f in v.Files)
        {
            Console.WriteLine("Type : " + f.Type);
            Console.WriteLine("AdUrl : " + f.AdUrl);
            Console.WriteLine("DirectUrl : " + f.DirectUrl);
            Console.WriteLine("MD5 : " + f.MD5);
            Console.WriteLine("SHA1 : " + f.SHA1);
        }
    }
    Console.WriteLine();
}