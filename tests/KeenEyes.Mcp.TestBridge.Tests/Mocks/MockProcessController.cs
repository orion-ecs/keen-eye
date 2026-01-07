using KeenEyes.TestBridge.Process;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IProcessController for testing.
/// </summary>
internal sealed class MockProcessController : IProcessController
{
    private readonly Dictionary<int, ProcessInfo> processes = [];
    private readonly Dictionary<int, string> stdoutBuffers = [];
    private readonly Dictionary<int, string> stderrBuffers = [];
    private int nextProcessId = 1000;

    public IReadOnlyList<ProcessInfo> RunningProcesses => processes.Values.ToList();

    public Task<ProcessInfo> StartAsync(ProcessStartOptions options, CancellationToken cancellationToken = default)
    {
        var processId = nextProcessId++;
        var info = new ProcessInfo
        {
            ProcessId = processId,
            Executable = options.Executable,
            Arguments = options.Arguments,
            WorkingDirectory = options.WorkingDirectory,
            HasExited = false,
            StartTime = DateTime.UtcNow
        };

        processes[processId] = info;
        stdoutBuffers[processId] = "";
        stderrBuffers[processId] = "";

        return Task.FromResult(info);
    }

    public Task<ProcessInfo> StartAsync(string executable, string? arguments = null, CancellationToken cancellationToken = default)
    {
        return StartAsync(new ProcessStartOptions
        {
            Executable = executable,
            Arguments = arguments
        }, cancellationToken);
    }

    public ProcessInfo? GetProcess(int processId)
    {
        return processes.GetValueOrDefault(processId);
    }

    public Task WriteLineAsync(int processId, string line, CancellationToken cancellationToken = default)
    {
        if (!processes.ContainsKey(processId))
        {
            throw new InvalidOperationException($"Process {processId} not found");
        }

        return Task.CompletedTask;
    }

    public string ReadStdout(int processId)
    {
        if (stdoutBuffers.TryGetValue(processId, out var output))
        {
            stdoutBuffers[processId] = "";
            return output;
        }

        return "";
    }

    public string ReadStderr(int processId)
    {
        if (stderrBuffers.TryGetValue(processId, out var output))
        {
            stderrBuffers[processId] = "";
            return output;
        }

        return "";
    }

    public string PeekStdout(int processId)
    {
        return stdoutBuffers.GetValueOrDefault(processId, "");
    }

    public string PeekStderr(int processId)
    {
        return stderrBuffers.GetValueOrDefault(processId, "");
    }

    public Task<ProcessExitResult> WaitForExitAsync(int processId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (processes.ContainsKey(processId))
        {
            return Task.FromResult(new ProcessExitResult
            {
                Completed = true,
                ExitCode = 0,
                Stdout = stdoutBuffers.GetValueOrDefault(processId, ""),
                Stderr = stderrBuffers.GetValueOrDefault(processId, ""),
                Duration = TimeSpan.FromSeconds(1)
            });
        }

        throw new InvalidOperationException($"Process {processId} not found");
    }

    public Task<bool> WaitForOutputAsync(int processId, string text, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task TerminateAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processes.TryGetValue(processId, out var info))
        {
            processes[processId] = info with { HasExited = true };
        }

        return Task.CompletedTask;
    }

    public Task KillAsync(int processId)
    {
        processes.Remove(processId);
        stdoutBuffers.Remove(processId);
        stderrBuffers.Remove(processId);
        return Task.CompletedTask;
    }

    public Task KillAllAsync()
    {
        processes.Clear();
        stdoutBuffers.Clear();
        stderrBuffers.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper to add stdout output for testing.
    /// </summary>
    public void AddStdout(int processId, string output)
    {
        if (stdoutBuffers.ContainsKey(processId))
        {
            stdoutBuffers[processId] += output;
        }
    }

    /// <summary>
    /// Helper to add stderr output for testing.
    /// </summary>
    public void AddStderr(int processId, string output)
    {
        if (stderrBuffers.ContainsKey(processId))
        {
            stderrBuffers[processId] += output;
        }
    }
}
