using System.Text;

namespace CmlLib.Utils;

internal static class IOUtil
{
    public static void CreateDirectoryForFile(string filepath)
    {
        var dir = Path.GetDirectoryName(filepath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }


    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
    }

    public static void DeleteDirectory(string targetDir)
    {
        try
        {
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    public static string CombinePath(IEnumerable<string> paths)
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

    public static async Task CopyDirectory(string org, string des, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dir = new DirectoryInfo(org);
        if (!dir.Exists)
            return;

        await copyDirectoryFiles(org, des, "", cancellationToken);
    }

    private static async Task copyDirectoryFiles(string org, string des, string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var orgpath = Path.Combine(org, path);
        var orgdir = new DirectoryInfo(orgpath);

        var despath = Path.Combine(des, path);
        if (!Directory.Exists(despath))
            Directory.CreateDirectory(despath);

        foreach (var dir in orgdir.GetDirectories("*", SearchOption.TopDirectoryOnly))
        {
            var innerpath = Path.Combine(path, dir.Name);
            await copyDirectoryFiles(org, des, innerpath, cancellationToken);
        }

        foreach (var file in orgdir.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            var innerpath = Path.Combine(path, file.Name);
            var desfile = Path.Combine(des, innerpath);

            await CopyFileAsync(file.FullName, desfile, cancellationToken);
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

    public static async Task CopyFileAsync(string source, string target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var sourceFile = File.OpenRead(source);
        using var targetFile = File.Create(target);
        await sourceFile.CopyToAsync(targetFile, 81920, cancellationToken);
    }

    public static async Task CopyFileAtomicAsync(string source, string target, CancellationToken cancellationToken)
    {
        var temporaryPath = target + "." + Path.GetRandomFileName();
        try
        {
            await CopyFileAsync(source, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(target))
                File.Replace(temporaryPath, target, null);
            else
                File.Move(temporaryPath, target);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException error)
            {
                System.Diagnostics.Debug.WriteLine(error);
            }
            catch (UnauthorizedAccessException error)
            {
                System.Diagnostics.Debug.WriteLine(error);
            }
        }
    }
}
