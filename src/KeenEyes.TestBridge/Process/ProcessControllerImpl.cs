using System.Collections.Concurrent;
using System.Diagnostics;
using KeenEyes.TestBridge.Process;
using KeenEyes.TestBridge.ProcessManagement;

namespace KeenEyes.TestBridge.ProcessImpl;

/// <summary>
/// Implementation of <see cref="IProcessController"/> for managing child processes.
/// </summary>
internal sealed class ProcessControllerImpl : IProcessController, IDisposable
{
    private readonly ConcurrentDictionary<int, ManagedProcess> processes = new();
    private bool disposed;

    public IReadOnlyList<ProcessInfo> RunningProcesses
    {
        get
        {
            var running = new List<ProcessInfo>();

            foreach (var kvp in processes)
            {
                var info = kvp.Value.GetInfo();
                if (!info.HasExited)
                {
                    running.Add(info);
                }
            }

            return running;
        }
    }

    public Task<ProcessInfo> StartAsync(string executable, string? arguments = null, CancellationToken cancellationToken = default)
    {
        return StartAsync(new ProcessStartOptions
        {
            Executable = executable,
            Arguments = arguments
        }, cancellationToken);
    }

    public Task<ProcessInfo> StartAsync(ProcessStartOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = options.Executable,
            Arguments = options.Arguments ?? string.Empty,
            WorkingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardInput = options.RedirectStdin,
            RedirectStandardOutput = options.RedirectStdout,
            RedirectStandardError = options.RedirectStderr,
            UseShellExecute = options.UseShellExecute,
            CreateNoWindow = options.CreateNoWindow
        };

        // Add environment variables
        if (options.EnvironmentVariables != null)
        {
            foreach (var kvp in options.EnvironmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        System.Diagnostics.Process process;

        try
        {
            process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process: {options.Executable}");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2) // ERROR_FILE_NOT_FOUND
        {
            throw new FileNotFoundException($"Executable not found: {options.Executable}", options.Executable, ex);
        }

        var managed = new ManagedProcess(process, options);
        processes[process.Id] = managed;

        return Task.FromResult(managed.GetInfo());
    }

    public ProcessInfo? GetProcess(int processId)
    {
        if (processes.TryGetValue(processId, out var managed))
        {
            return managed.GetInfo();
        }

        return null;
    }

    public async Task WriteLineAsync(int processId, string line, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!processes.TryGetValue(processId, out var managed))
        {
            throw new InvalidOperationException($"Process {processId} is not managed by this controller.");
        }

        await managed.WriteLineAsync(line, cancellationToken);
    }

    public string ReadStdout(int processId)
    {
        if (!processes.TryGetValue(processId, out var managed))
        {
            return string.Empty;
        }

        return managed.ReadStdout();
    }

    public string ReadStderr(int processId)
    {
        if (!processes.TryGetValue(processId, out var managed))
        {
            return string.Empty;
        }

        return managed.ReadStderr();
    }

    public string PeekStdout(int processId)
    {
        if (!processes.TryGetValue(processId, out var managed))
        {
            return string.Empty;
        }

        return managed.PeekStdout();
    }

    public string PeekStderr(int processId)
    {
        if (!processes.TryGetValue(processId, out var managed))
        {
            return string.Empty;
        }

        return managed.PeekStderr();
    }

    public async Task<ProcessExitResult> WaitForExitAsync(int processId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!processes.TryGetValue(processId, out var managed))
        {
            throw new InvalidOperationException($"Process {processId} is not managed by this controller.");
        }

        return await managed.WaitForExitAsync(timeout, cancellationToken);
    }

    public async Task<bool> WaitForOutputAsync(int processId, string text, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!processes.TryGetValue(processId, out var managed))
        {
            return false;
        }

        return await managed.WaitForOutputAsync(text, timeout, cancellationToken);
    }

    public async Task TerminateAsync(int processId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!processes.TryGetValue(processId, out var managed))
        {
            return;
        }

        await managed.TerminateAsync();
    }

    public async Task KillAsync(int processId)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!processes.TryGetValue(processId, out var managed))
        {
            return;
        }

        await managed.KillAsync();
    }

    public async Task KillAllAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var tasks = new List<Task>();

        foreach (var kvp in processes)
        {
            tasks.Add(kvp.Value.KillAsync());
        }

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var kvp in processes)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        processes.Clear();
    }
}
