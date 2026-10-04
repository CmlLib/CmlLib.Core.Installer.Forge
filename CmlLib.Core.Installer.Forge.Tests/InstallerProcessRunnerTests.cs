using System.Diagnostics;
using System.Text.Json;
using CmlLib.Core.Installer.Forge.Internal;
using CmlLib.Core.Installers;

namespace CmlLib.Core.Installer.Forge.Tests;

public sealed class InstallerProcessRunnerTests
{
    [Theory]
    [InlineData(InstallerEventType.Queued)]
    [InlineData(InstallerEventType.Done)]
    public async Task CancellationFromProgressIsObservedBeforeReturning(InstallerEventType cancelAt)
    {
        using var cancellation = new CancellationTokenSource();
        // Queued cancellation must win over opening the nonexistent processor JAR.
        var outputs = cancelAt == InstallerEventType.Done ? ",\"outputs\":{}" : "";
        using var processors = JsonDocument.Parse("[{\"jar\":\"fixture:missing:1\"" + outputs + "}]");
        var progress = new InlineProgress<InstallerProgressChangedEventArgs>(value =>
        {
            if (value.EventType == cancelAt)
                cancellation.Cancel();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ForgeInstallProcessor("unused").StartProcessors(
            processors.RootElement, new Dictionary<string, string?>(), "unused", progress, null, cancellation.Token));
    }

    [Fact]
    public async Task CancellationTerminatesTheRunningProcessBeforeReturning()
    {
        using var cancellation = new CancellationTokenSource();
        using var process = CreateShellProcess(OperatingSystem.IsWindows()
            ? "echo ready & for /L %i in (1,1,2147483647) do @rem"
            : "echo ready; while :; do :; done");
        var output = new InlineProgress<string>(line =>
        {
            if (line.Trim() == "ready")
                cancellation.Cancel();
        });

        try
        {
            var task = InstallerProcessRunner.Run(process, output, cancellation.Token);
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => task.WaitAsync(TimeSpan.FromSeconds(10)));

            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Null(error.InnerException);
            Assert.True(process.HasExited);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
    }

    [UnixTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationTimesOutWhenChildKeepsOutputPipesOpen(bool parentAlreadyExited)
    {
        using var cancellation = new CancellationTokenSource();
        using var process = CreateShellProcess("sleep 30 & echo child:$!; " + (parentAlreadyExited ? "exit 0" : "wait"));
        var childId = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new InlineProgress<string>(line =>
        {
            if (line.StartsWith("child:", StringComparison.Ordinal))
                childId.TrySetResult(int.Parse(line.Substring("child:".Length)));
        });
        Process? child = null;
        var task = InstallerProcessRunner.Run(process, output, cancellation.Token);
        try
        {
            child = Process.GetProcessById(await childId.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            if (parentAlreadyExited)
                Assert.True(SpinWait.SpinUntil(() => process.HasExited, TimeSpan.FromSeconds(5)));

            cancellation.Cancel();
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => task.WaitAsync(TimeSpan.FromSeconds(12)));

            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.IsType<TimeoutException>(error.InnerException);
            Assert.True(process.HasExited);
            Assert.False(child.HasExited);
            Assert.True(task.IsCanceled);
        }
        finally
        {
            // Close the inherited write handles and leave no fixture processes behind.
            if (child == null && childId.Task.IsCompletedSuccessfully)
                child = Process.GetProcessById(childId.Task.Result);
            if (child != null)
            {
                using (child)
                {
                    if (!child.HasExited)
                        child.Kill();
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            if (!process.HasExited)
                process.Kill();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task AlreadyCanceledTokenDoesNotStartProcess()
    {
        using var process = new Process { StartInfo = new ProcessStartInfo("nonexistent-review-processor") };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InstallerProcessRunner.Run(process, null, cancellation.Token));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task NormalExitReturnsExitCode(int exitCode)
    {
        using var process = CreateShellProcess($"exit {exitCode}");

        Assert.Equal(exitCode, await InstallerProcessRunner.Run(process, null, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10)));
    }

    private static Process CreateShellProcess(string command)
    {
        var startInfo = new ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh");
        startInfo.ArgumentList.Add(OperatingSystem.IsWindows() ? "/c" : "-c");
        startInfo.ArgumentList.Add(command);
        return new Process { StartInfo = startInfo };
    }
}

internal sealed class InlineProgress<T> : IProgress<T>
{
    private readonly Action<T> _report;
    public InlineProgress(Action<T> report) => _report = report;
    public void Report(T value) => _report(value);
}

internal sealed class UnixTheoryAttribute : TheoryAttribute
{
    public UnixTheoryAttribute()
    {
        if (OperatingSystem.IsWindows())
            Skip = "The inherited-pipe fixture requires a Unix shell.";
    }
}
