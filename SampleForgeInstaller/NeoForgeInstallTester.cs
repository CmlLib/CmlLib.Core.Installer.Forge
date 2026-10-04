using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.FileExtractors;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;

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

        // Each target gets its own game, versions, and libraries directories.
        // Assets and Java runtimes are shared from the cache.
        var sharedPath = new MinecraftPath(cache);
        var reportPath = Path.Combine(output, "installation-results.json");
        var priorResults = resume && File.Exists(reportPath)
            ? JsonSerializer.Deserialize<InstallationResult[]>(await File.ReadAllTextAsync(reportPath)) ?? Array.Empty<InstallationResult>()
            : Array.Empty<InstallationResult>();
        var completedVersions = priorResults
            .Where(result => result.Status == "passed")
            .Select(result => result.MinecraftVersion)
            .ToHashSet(StringComparer.Ordinal);
        targets = targets.Where(target => !completedVersions.Contains(target.MinecraftVersionName)).ToArray();
        var scheduledVersions = targets.Select(target => target.MinecraftVersionName).ToHashSet(StringComparer.Ordinal);
        var results = priorResults
            .Where(result => !scheduledVersions.Contains(result.MinecraftVersion))
            .ToList();

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
                var gamePath = new MinecraftPath(directory)
                {
                    Assets = sharedPath.Assets,
                    Runtime = sharedPath.Runtime
                };
                var parameters = MinecraftLauncherParameters.CreateDefault(gamePath, http);
                var launcher = new MinecraftLauncher(parameters);
                var installer = new NeoForgeInstaller(launcher, http);
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
                // Complete all game files and Java runtime before launching.
                await launcher.InstallAsync(id);
                var libraryCount = await ValidateInstalledAsync(directory, target, id);
                var startupMarker = await LaunchAndConfirmStartupAsync(launcher, id, log);
                results.Add(new(target.MinecraftVersionName, target.NeoForgeVersionName, "passed", directory,
                    libraryCount, timer.Elapsed.TotalSeconds, startupMarker, null));
                Console.WriteLine($"PASS {folderName} ({libraryCount} libraries; startup: {startupMarker}; {timer.Elapsed.TotalSeconds:F1}s)");
            }
            catch (Exception error)
            {
                log.WriteLine(error);
                results.Add(new(target.MinecraftVersionName, target.NeoForgeVersionName, "failed", directory,
                    0, timer.Elapsed.TotalSeconds, null, error.ToString()));
                Console.WriteLine($"FAIL {folderName}: {error.Message} (see {logPath})");
            }
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(results, JsonOptions));
        }

        var failed = results.Count(result => result.Status == "failed");
        Console.WriteLine($"RESULT {results.Count - failed}/{results.Count} passed, {failed} failed. Report: {reportPath}");
        return failed == 0 ? 0 : 1;
    }

    private static async Task<int> ValidateInstalledAsync(
        string gameDirectory, NeoForgeVersion target, string installedId)
    {
        var versionDirectory = Path.Combine(gameDirectory, "versions", installedId);
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
            var source = ResolvePath(Path.Combine(gameDirectory, "libraries"), relative);
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
        return files.Count;
    }

    private static async Task<string> LaunchAndConfirmStartupAsync(
        MinecraftLauncher launcher, string installedId, TextWriter log)
    {
        var process = await launcher.BuildProcessAsync(installedId, new MLaunchOption
        {
            Session = MSession.CreateOfflineSession("NeoForgeTester"),
            MaximumRamMb = 3072
        });
        var startup = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var wrapper = new ProcessWrapper(process);
        wrapper.OutputReceived += (_, line) =>
        {
            log.WriteLine(line);
            if (line.Contains("Sound engine started", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Started SoundEngine", StringComparison.OrdinalIgnoreCase))
                startup.TrySetResult(ExtractLogMessage(line));
        };
        wrapper.StartWithEvents();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var exited = process.WaitForExitAsync();
        var completed = await Task.WhenAny(startup.Task, exited, Task.Delay(Timeout.Infinite, timeout.Token));
        if (completed == startup.Task)
        {
            var marker = await startup.Task;
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return marker;
        }

        if (completed == exited)
        {
            var exitCode = process.ExitCode;
            if (startup.Task.IsCompletedSuccessfully)
                return startup.Task.Result;
            throw new InvalidOperationException($"Minecraft exited with code {exitCode} before the startup marker.");
        }

        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        throw new TimeoutException("Minecraft did not reach the sound-engine startup marker within 90 seconds.");
    }

    private static string ExtractLogMessage(string line)
    {
        const string cdataStart = "<![CDATA[";
        var start = line.IndexOf(cdataStart, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += cdataStart.Length;
            var end = line.IndexOf("]]>", start, StringComparison.Ordinal);
            if (end >= 0)
                return line[start..end].Trim();
        }
        return line.Trim();
    }

    private static string ResolvePath(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal))
            throw new InvalidDataException($"Path escapes its directory: {relative}");
        return fullPath;
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
        string Directory, int LibraryCount, double ElapsedSeconds, string? StartupMarker, string? Error);
}
