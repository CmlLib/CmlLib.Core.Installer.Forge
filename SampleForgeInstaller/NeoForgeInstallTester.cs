using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using CmlLib.Core;
using CmlLib.Core.FileExtractors;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installers;

namespace SampleForgeInstaller;

internal sealed class NeoForgeInstallTester
{
    // Keep this list editable independently of catalog discovery.
    private static readonly string[] MinecraftVersions =
    {
        "26.3", "26.2", "26.1.2", "26.1.1", "26.1",
        "1.21.11", "1.21.10", "1.21.9", "1.21.8", "1.21.7", "1.21.6",
        "1.21.5", "1.21.4", "1.21.3", "1.21.2", "1.21.1", "1.21.0",
        "1.20.6", "1.20.5", "1.20.4", "1.20.3", "1.20.2"
    };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<int> RunAsync(string[] args)
    {
        var output = Path.GetFullPath(GetOption(args, "--output") ?? "neoforge-installations");
        var cache = Path.GetFullPath(GetOption(args, "--cache") ?? output + ".cache");
        var java = GetOption(args, "--java");
        var minecraft = GetOption(args, "--minecraft");
        var resume = args.Contains("--resume");
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(cache);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        var planPath = Path.Combine(cache, "installation-plan.json");
        IReadOnlyList<NeoForgeVersion> targets;
        if (resume)
        {
            targets = JsonSerializer.Deserialize<NeoForgeVersion[]>(await File.ReadAllTextAsync(planPath))
                ?? throw new InvalidDataException("Missing saved installation plan.");
        }
        else
        {
            var allVersions = await new NeoForgeVersionLoader(http).GetNeoForgeVersions();
            targets = MinecraftVersions.Select(name =>
            {
                var normalized = NeoForgeVersionMapper.NormalizeMinecraftVersion(name);
                return allVersions.FirstOrDefault(version => version.MinecraftVersionName == normalized)
                    ?? throw new InvalidOperationException($"No NeoForge release for Minecraft {name}.");
            }).ToArray();
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(targets, JsonOptions));
        }
        if (minecraft != null)
        {
            var normalized = NeoForgeVersionMapper.NormalizeMinecraftVersion(minecraft);
            targets = targets.Where(version => version.MinecraftVersionName == normalized).ToArray();
            if (targets.Count == 0)
                throw new ArgumentException($"Minecraft {minecraft} is not in the installation plan.");
        }
        foreach (var target in targets)
            Console.WriteLine($"PLAN Minecraft {target.MinecraftVersionName} -> NeoForge {target.NeoForgeVersionName}");
        if (args.Contains("--plan-only"))
            return 0;

        // Install vanilla client/libraries/Java required by processors, without game assets.
        var parameters = MinecraftLauncherParameters.CreateDefault(new MinecraftPath(cache), http);
        var extractors = DefaultFileExtractors.CreateDefault(http, parameters.RulesEvaluator!, parameters.JavaPathResolver!);
        extractors.Asset = null;
        extractors.Log = null;
        parameters.FileExtractors = extractors.ToExtractorCollection();
        var launcher = new MinecraftLauncher(parameters);
        var installer = new NeoForgeInstaller(launcher, http);
        var results = new List<InstallationResult>();
        var reportPath = Path.Combine(output, "installation-results.json");

        foreach (var target in targets)
        {
            var folderName = $"minecraft-{target.MinecraftVersionName}-neoforge-{target.NeoForgeVersionName}";
            if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException($"Invalid output directory name: {folderName}");
            var directory = Path.Combine(output, folderName);
            var timer = Stopwatch.StartNew();
            var logPath = Path.Combine(cache, folderName + ".log");
            using var log = TextWriter.Synchronized(new StreamWriter(logPath, append: resume) { AutoFlush = true });
            Console.WriteLine($"START {folderName}");
            try
            {
                var id = await installer.Install(target, new ForgeInstallOptions
                {
                    JavaPath = java,
                    SkipIfAlreadyInstalled = resume,
                    FileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(progress =>
                        log.WriteLine($"[{progress.EventType}] {progress.Name}")),
                    InstallerOutput = new SyncProgress<string>(line =>
                    {
                        log.WriteLine(line);
                    })
                });
                // Complete libraries declared by version.json before checking generated artifacts.
                await launcher.InstallAsync(id);
                var libraryCount = await ValidateAndExportAsync(cache, directory, target, id);
                results.Add(new(target.MinecraftVersionName, target.NeoForgeVersionName, "passed", directory,
                    libraryCount, timer.Elapsed.TotalSeconds, null));
                Console.WriteLine($"PASS {folderName} ({libraryCount} libraries, {timer.Elapsed.TotalSeconds:F1}s)");
            }
            catch (Exception error)
            {
                log.WriteLine(error);
                results.Add(new(target.MinecraftVersionName, target.NeoForgeVersionName, "failed", directory,
                    0, timer.Elapsed.TotalSeconds, error.ToString()));
                Console.WriteLine($"FAIL {folderName}: {error.Message} (see {logPath})");
            }
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(results, JsonOptions));
        }

        var failed = results.Count(result => result.Status == "failed");
        Console.WriteLine($"RESULT {results.Count - failed}/{results.Count} passed, {failed} failed. Report: {reportPath}");
        return failed == 0 ? 0 : 1;
    }

    private static async Task<int> ValidateAndExportAsync(
        string cache, string output, NeoForgeVersion target, string installedId)
    {
        var versionDirectory = Path.Combine(cache, "versions", installedId);
        var jsonPath = Path.Combine(versionDirectory, installedId + ".json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
        var root = document.RootElement;
        if (root.GetProperty("id").GetString() != installedId ||
            root.GetProperty("inheritsFrom").GetString() != target.MinecraftVersionName)
            throw new InvalidDataException("Installed version metadata does not match its target.");

        var files = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var library in root.GetProperty("libraries").EnumerateArray())
        {
            if (library.TryGetProperty("downloads", out var downloads) &&
                downloads.TryGetProperty("artifact", out var artifact))
            {
                files[artifact.GetProperty("path").GetString()!] =
                    artifact.TryGetProperty("sha1", out var sha1) ? sha1.GetString() : null;
            }
            else
            {
                var coordinate = library.GetProperty("name").GetString()!;
                var extension = coordinate.Split('@');
                var type = "jar";
                if (extension.Length > 1)
                    type = extension[1];
                files[ForgePackageName.GetPath(extension[0], type, '/')] = null;
            }
        }

        // Check every declared loader library, including processor-generated jars.
        foreach (var (relative, hash) in files)
        {
            var source = ResolvePath(Path.Combine(cache, "libraries"), relative);
            if (!File.Exists(source) || new FileInfo(source).Length == 0)
                throw new FileNotFoundException("NeoForge runtime library is missing or empty.", source);
            if (!string.IsNullOrEmpty(hash))
            {
                using var input = File.OpenRead(source);
                using var sha1 = SHA1.Create();
                var actual = Convert.ToHexString(sha1.ComputeHash(input));
                if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Library checksum mismatch: {source}");
            }
        }
        foreach (var relative in files.Keys)
            CopyFile(ResolvePath(Path.Combine(cache, "libraries"), relative),
                ResolvePath(Path.Combine(output, "libraries"), relative));
        foreach (var file in Directory.EnumerateFiles(versionDirectory, "*", SearchOption.AllDirectories))
            CopyFile(file, ResolvePath(Path.Combine(output, "versions", installedId), Path.GetRelativePath(versionDirectory, file)));
        return files.Count;
    }

    private static string ResolvePath(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal))
            throw new InvalidDataException($"Path escapes its directory: {relative}");
        return fullPath;
    }

    private static void CopyFile(string source, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
    }

    private static string? GetOption(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        if (index < 0)
            return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Missing value for {option}.");
        return args[index + 1];
    }

    private sealed record InstallationResult(string MinecraftVersion, string NeoForgeVersion, string Status,
        string Directory, int LibraryCount, double ElapsedSeconds, string? Error);
}
