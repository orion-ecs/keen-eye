using System.Diagnostics;
using System.Text;
using KeenEyes.TestBridge.Process;

namespace KeenEyes.TestBridge.ProcessManagement;

/// <summary>
/// Internal wrapper for System.Diagnostics.Process that manages output buffering
/// and provides thread-safe access to process state.
/// </summary>
internal sealed class ManagedProcess : IDisposable
{
    private readonly System.Diagnostics.Process process;
    private readonly ProcessStartOptions options;
    private readonly StringBuilder stdoutBuffer = new();
    private readonly StringBuilder stderrBuffer = new();
    private readonly StringBuilder stdoutFullHistory = new();
    private readonly StringBuilder stderrFullHistory = new();
    private readonly Lock bufferLock = new();
    private readonly TaskCompletionSource<int> exitTcs = new();
    private readonly DateTime startTime;
    private readonly Task? stdoutReadTask;
    private readonly Task? stderrReadTask;

    private bool disposed;

    public ManagedProcess(System.Diagnostics.Process process, ProcessStartOptions options)
    {
        this.process = process;
        this.options = options;
        startTime = DateTime.UtcNow;

        if (options.RedirectStdout)
        {
            stdoutReadTask = ReadOutputAsync(process.StandardOutput, stdoutBuffer, stdoutFullHistory, options.MaxStdoutBuffer);
        }

        if (options.RedirectStderr)
        {
            stderrReadTask = ReadOutputAsync(process.StandardError, stderrBuffer, stderrFullHistory, options.MaxStderrBuffer);
        }

        // Monitor for exit
        _ = MonitorExitAsync();
    }

    public int ProcessId => process.Id;

    public string Executable => options.Executable;

    public string? Arguments => options.Arguments;

    public string? WorkingDirectory => options.WorkingDirectory;

    public bool HasExited
    {
        get
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }

    public int? ExitCode
    {
        get
        {
            try
            {
                return HasExited ? process.ExitCode : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    public DateTime StartTime => startTime;

    public DateTime? ExitTime => HasExited ? DateTime.UtcNow : null;

    public ProcessInfo GetInfo()
    {
        return new ProcessInfo
        {
            ProcessId = ProcessId,
            Executable = Executable,
            Arguments = Arguments,
            WorkingDirectory = WorkingDirectory,
            HasExited = HasExited,
            ExitCode = ExitCode,
            StartTime = StartTime,
            ExitTime = ExitTime
        };
    }

    public async Task WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        if (!options.RedirectStdin)
        {
            throw new InvalidOperationException("Standard input is not redirected for this process.");
        }

        if (HasExited)
        {
            throw new InvalidOperationException("Cannot write to a process that has exited.");
        }

        await process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    public string ReadStdout()
    {
        lock (bufferLock)
        {
            var result = stdoutBuffer.ToString();
            stdoutBuffer.Clear();
            return result;
        }
    }

    public string ReadStderr()
    {
        lock (bufferLock)
        {
            var result = stderrBuffer.ToString();
            stderrBuffer.Clear();
            return result;
        }
    }

    public string PeekStdout()
    {
        lock (bufferLock)
        {
            return stdoutBuffer.ToString();
        }
    }

    public string PeekStderr()
    {
        lock (bufferLock)
        {
            return stderrBuffer.ToString();
        }
    }

    public string GetFullStdout()
    {
        lock (bufferLock)
        {
            return stdoutFullHistory.ToString();
        }
    }

    public string GetFullStderr()
    {
        lock (bufferLock)
        {
            return stderrFullHistory.ToString();
        }
    }

    public async Task<ProcessExitResult> WaitForExitAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        if (timeout.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout.Value);

            try
            {
                await exitTcs.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                return new ProcessExitResult
                {
                    Completed = false,
                    ExitCode = null,
                    Stdout = GetFullStdout(),
                    Stderr = GetFullStderr(),
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }
        else
        {
            await exitTcs.Task.WaitAsync(cancellationToken);
        }

        // Wait for output reading to complete
        if (stdoutReadTask != null)
        {
            await stdoutReadTask;
        }

        if (stderrReadTask != null)
        {
            await stderrReadTask;
        }

        return new ProcessExitResult
        {
            Completed = true,
            ExitCode = ExitCode,
            Stdout = GetFullStdout(),
            Stderr = GetFullStderr(),
            Duration = DateTime.UtcNow - startTime
        };
    }

    public async Task<bool> WaitForOutputAsync(string text, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                lock (bufferLock)
                {
                    if (stdoutFullHistory.ToString().Contains(text, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                if (HasExited)
                {
                    // Check one more time after exit
                    lock (bufferLock)
                    {
                        return stdoutFullHistory.ToString().Contains(text, StringComparison.Ordinal);
                    }
                }

                await Task.Delay(50, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout - check one final time
            lock (bufferLock)
            {
                return stdoutFullHistory.ToString().Contains(text, StringComparison.Ordinal);
            }
        }

        return false;
    }

    public Task TerminateAsync()
    {
        if (HasExited)
        {
            return Task.CompletedTask;
        }

        // On Windows, we try to close the main window first for graceful shutdown
        // On Unix, Process.Kill() sends SIGTERM by default in .NET
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Try graceful shutdown first
                process.CloseMainWindow();
            }
            else
            {
                // On Unix, Kill() sends SIGTERM
                process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }

        return Task.CompletedTask;
    }

    public Task KillAsync()
    {
        if (HasExited)
        {
            return Task.CompletedTask;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            if (!HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited, which is fine during cleanup
        }
        catch (SystemException)
        {
            // Other system errors during kill are acceptable during cleanup
        }

        process.Dispose();
    }

    private async Task MonitorExitAsync()
    {
        try
        {
            await process.WaitForExitAsync();
            exitTcs.TrySetResult(process.ExitCode);
        }
        catch (Exception ex)
        {
            exitTcs.TrySetException(ex);
        }
    }

    private async Task ReadOutputAsync(StreamReader reader, StringBuilder buffer, StringBuilder fullHistory, int maxBuffer)
    {
        var charBuffer = new char[4096];

        try
        {
            int charsRead;
            while ((charsRead = await reader.ReadAsync(charBuffer, CancellationToken.None)) > 0)
            {
                var text = new string(charBuffer, 0, charsRead);

                lock (bufferLock)
                {
                    buffer.Append(text);
                    fullHistory.Append(text);

                    // Trim buffer if exceeds max size (FIFO - remove oldest)
                    if (buffer.Length > maxBuffer)
                    {
                        var excess = buffer.Length - maxBuffer;
                        buffer.Remove(0, excess);
                    }

                    // Trim full history too to prevent unbounded memory growth
                    if (fullHistory.Length > maxBuffer * 2)
                    {
                        var excess = fullHistory.Length - maxBuffer;
                        fullHistory.Remove(0, excess);
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Stream was closed, normal during shutdown
        }
        catch (IOException)
        {
            // IO error, process likely terminated
        }
    }
}
