using CmlLib.Core.ProcessBuilder;
using System.ComponentModel;
using System.Diagnostics;

namespace CmlLib.Core.Installer.Forge.Internal;

internal static class InstallerProcessRunner
{
    private static readonly TimeSpan CancellationTimeout = TimeSpan.FromSeconds(5);

    public static async Task<int> Run(
        Process process,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var wrapper = new ProcessWrapper(process);
        EventHandler<string> onOutput = (_, line) => output?.Report(line);
        wrapper.OutputReceived += onOutput;
        try
        {
            wrapper.StartWithEvents();

            var exited = wrapper.WaitForExitTaskAsync();
            var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => canceled.TrySetResult(true)))
            {
                if (await Task.WhenAny(exited, canceled.Task) == canceled.Task)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException) when (process.HasExited)
                    {
                        // The processor exited while cancellation was being handled.
                    }
                    catch (Win32Exception) when (process.HasExited)
                    {
                        // Some platforms report the same exit race as a native error.
                    }

                    using var timeoutCancellation = new CancellationTokenSource();
                    var timeout = Task.Delay(CancellationTimeout, timeoutCancellation.Token);
                    if (await Task.WhenAny(exited, timeout) != exited)
                    {
                        // The caller disposes the Process. Observe any resulting failure in the pending wait.
                        _ = ObserveExit(exited);
                        throw new OperationCanceledException(
                            "Installer processor cancellation timed out while waiting for process exit and redirected output.",
                            new TimeoutException($"The processor did not finish shutting down within {CancellationTimeout.TotalSeconds} seconds."),
                            cancellationToken);
                    }
                    timeoutCancellation.Cancel();
                }

                var exitCode = await exited;
                cancellationToken.ThrowIfCancellationRequested();
                return exitCode;
            }
        }
        finally
        {
            wrapper.OutputReceived -= onOutput;
        }
    }

    private static async Task ObserveExit(Task exited)
    {
        try
        {
            await exited;
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
        }
    }
}
