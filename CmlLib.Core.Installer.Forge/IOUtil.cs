namespace CmlLib.Core.Installer.Forge;

internal class IOUtil
{
    public static void CopyDirectory(string org, string des, bool overwrite)
    {
        var dir = new DirectoryInfo(org);
        if (!dir.Exists)
            return;

        copyDirectoryFiles(org, des, "", overwrite);
    }

    private static void copyDirectoryFiles(string org, string des, string path, bool overwrite)
    {
        var orgpath = Path.Combine(org, path);
        var orgdir = new DirectoryInfo(orgpath);

        var despath = Path.Combine(des, path);
        if (!Directory.Exists(despath))
            Directory.CreateDirectory(despath);

        foreach (var dir in orgdir.GetDirectories("*", SearchOption.TopDirectoryOnly))
        {
            var innerpath = Path.Combine(path, dir.Name);
            copyDirectoryFiles(org, des, innerpath, overwrite);
        }

        foreach (var file in orgdir.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            var innerpath = Path.Combine(path, file.Name);
            var desfile = Path.Combine(des, innerpath);

            file.CopyTo(desfile, overwrite);
        }
    }

    public static bool CheckSHA1(string path, string? compareHash)
    {
        if (string.IsNullOrEmpty(compareHash))
            return true;

        try
        {
            string fileHash;

            using (var file = File.OpenRead(path))
            using (var hasher = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                var binaryHash = hasher.ComputeHash(file);
                fileHash = BitConverter.ToString(binaryHash).Replace("-", "").ToLowerInvariant();
            }

            return fileHash == compareHash;
        }
        catch
        {
            return false;
        }
    }

    public static string CombinePath(string[] paths)
    {
        return string.Join(Path.PathSeparator.ToString(),
            paths.Select(x =>
            {
                string path = Path.GetFullPath(x);
                if (path.Contains(' '))
                    return "\"" + path + "\"";
                else
                    return path;
            }));
    }
}
