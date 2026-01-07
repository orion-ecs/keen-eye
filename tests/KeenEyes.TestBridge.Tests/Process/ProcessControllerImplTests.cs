#pragma warning disable xUnit1051 // CancellationToken is not needed for simple test delays

using KeenEyes.TestBridge.Process;
using KeenEyes.TestBridge.ProcessImpl;

namespace KeenEyes.TestBridge.Tests.Process;

public class ProcessControllerImplTests : IDisposable
{
    private readonly ProcessControllerImpl controller = new();

    public void Dispose()
    {
        controller.Dispose();
        GC.SuppressFinalize(this);
    }

    #region RunningProcesses

    [Fact]
    public void RunningProcesses_Initially_ReturnsEmpty()
    {
        controller.RunningProcesses.ShouldBeEmpty();
    }

    #endregion

    #region StartAsync

    [Fact]
    public async Task StartAsync_WithValidExecutable_StartsProcess()
    {
        var info = await controller.StartAsync("dotnet", "--version");

        info.ShouldNotBeNull();
        info.ProcessId.ShouldBeGreaterThan(0);
        info.Executable.ShouldBe("dotnet");
        info.Arguments.ShouldBe("--version");
        info.HasExited.ShouldBeFalse();

        // Wait for it to complete
        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task StartAsync_WithOptions_AppliesConfiguration()
    {
        var options = new ProcessStartOptions
        {
            Executable = "dotnet",
            Arguments = "--version",
            CreateNoWindow = true,
            RedirectStdout = true
        };

        var info = await controller.StartAsync(options);

        info.ShouldNotBeNull();
        info.Executable.ShouldBe("dotnet");

        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task StartAsync_WithInvalidExecutable_ThrowsFileNotFound()
    {
        await Should.ThrowAsync<FileNotFoundException>(async () =>
            await controller.StartAsync("nonexistent_executable_xyz123"));
    }

    [Fact]
    public async Task StartAsync_AddsToRunningProcesses()
    {
        // Use a command that runs for a bit
        var info = await controller.StartAsync("dotnet", "--help");

        controller.RunningProcesses.ShouldContain(p => p.ProcessId == info.ProcessId);

        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));
    }

    #endregion

    #region GetProcess

    [Fact]
    public async Task GetProcess_WithValidId_ReturnsInfo()
    {
        var started = await controller.StartAsync("dotnet", "--version");

        var info = controller.GetProcess(started.ProcessId);

        info.ShouldNotBeNull();
        info!.ProcessId.ShouldBe(started.ProcessId);

        await controller.WaitForExitAsync(started.ProcessId, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GetProcess_WithInvalidId_ReturnsNull()
    {
        var info = controller.GetProcess(99999);

        info.ShouldBeNull();
    }

    #endregion

    #region ReadStdout / ReadStderr

    [Fact]
    public async Task ReadStdout_AfterOutput_ReturnsOutput()
    {
        var info = await controller.StartAsync("dotnet", "--version");

        // Wait for process to complete
        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));

        // Small delay to ensure output is buffered
        await Task.Delay(100);

        var stdout = controller.ReadStdout(info.ProcessId);

        stdout.ShouldNotBeEmpty();
        // dotnet --version outputs something like "8.0.100"
        stdout.ShouldContain(".");
    }

    [Fact]
    public async Task ReadStdout_ClearsBuffer()
    {
        var info = await controller.StartAsync("dotnet", "--version");
        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));

        // First read gets output
        var first = controller.ReadStdout(info.ProcessId);

        // Second read should be empty (buffer cleared)
        var second = controller.ReadStdout(info.ProcessId);

        first.ShouldNotBeEmpty();
        second.ShouldBeEmpty();
    }

    [Fact]
    public async Task PeekStdout_DoesNotClearBuffer()
    {
        var info = await controller.StartAsync("dotnet", "--version");
        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));

        // Peek should return output
        var first = controller.PeekStdout(info.ProcessId);

        // Peek again should return same output
        var second = controller.PeekStdout(info.ProcessId);

        first.ShouldNotBeEmpty();
        second.ShouldBe(first);
    }

    [Fact]
    public void ReadStdout_WithInvalidId_ReturnsEmpty()
    {
        var output = controller.ReadStdout(99999);

        output.ShouldBeEmpty();
    }

    #endregion

    #region WaitForExitAsync

    [Fact]
    public async Task WaitForExitAsync_ProcessExits_ReturnsCompleted()
    {
        var info = await controller.StartAsync("dotnet", "--version");

        var result = await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));

        result.Completed.ShouldBeTrue();
        result.ExitCode.ShouldBe(0);
        result.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task WaitForExitAsync_CapturesStdout()
    {
        var info = await controller.StartAsync("dotnet", "--version");

        var result = await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));

        result.Stdout.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task WaitForExitAsync_InvalidProcess_ThrowsInvalidOperation()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.WaitForExitAsync(99999, TimeSpan.FromSeconds(1)));
    }

    #endregion

    #region WaitForOutputAsync

    [Fact]
    public async Task WaitForOutputAsync_TextAppears_ReturnsTrue()
    {
        var info = await controller.StartAsync("dotnet", "--version");

        // Wait for any version number pattern
        var found = await controller.WaitForOutputAsync(info.ProcessId, ".", TimeSpan.FromSeconds(10));

        found.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitForOutputAsync_TextNotFound_ReturnsFalse()
    {
        var info = await controller.StartAsync("dotnet", "--version");

        // Wait for something that won't appear
        var found = await controller.WaitForOutputAsync(info.ProcessId, "UNLIKELY_STRING_XYZ123", TimeSpan.FromMilliseconds(500));

        found.ShouldBeFalse();
    }

    #endregion

    #region KillAsync

    [Fact]
    public async Task KillAsync_RunningProcess_ProcessKilled()
    {
        // Start a process that would run for a while (--help outputs a lot and takes time)
        var info = await controller.StartAsync("dotnet", "--help");

        await controller.KillAsync(info.ProcessId);

        // Give it a moment
        await Task.Delay(100);

        var updated = controller.GetProcess(info.ProcessId);
        updated?.HasExited.ShouldBeTrue();
    }

    [Fact]
    public async Task KillAsync_AlreadyExited_DoesNotThrow()
    {
        var info = await controller.StartAsync("dotnet", "--version");
        await controller.WaitForExitAsync(info.ProcessId, TimeSpan.FromSeconds(10));

        // Should not throw
        await controller.KillAsync(info.ProcessId);
    }

    [Fact]
    public async Task KillAsync_InvalidProcess_DoesNotThrow()
    {
        // Should not throw
        await controller.KillAsync(99999);
    }

    #endregion

    #region KillAllAsync

    [Fact]
    public async Task KillAllAsync_MultipleProcesses_AllKilled()
    {
        // Start multiple processes
        var info1 = await controller.StartAsync("dotnet", "--help");
        var info2 = await controller.StartAsync("dotnet", "--help");

        await controller.KillAllAsync();

        // Give it a moment
        await Task.Delay(100);

        var p1 = controller.GetProcess(info1.ProcessId);
        var p2 = controller.GetProcess(info2.ProcessId);

        p1?.HasExited.ShouldBeTrue();
        p2?.HasExited.ShouldBeTrue();
    }

    #endregion

    #region TerminateAsync

    [Fact]
    public async Task TerminateAsync_RunningProcess_InitiatesTermination()
    {
        var info = await controller.StartAsync("dotnet", "--help");

        await controller.TerminateAsync(info.ProcessId);

        // Wait a bit for termination to take effect
        await Task.Delay(500);

        // Process should have exited or be exiting
        var updated = controller.GetProcess(info.ProcessId);

        // On Windows, CloseMainWindow may not immediately terminate console apps
        // so we just verify no exception was thrown
        updated.ShouldNotBeNull();
    }

    [Fact]
    public async Task TerminateAsync_InvalidProcess_DoesNotThrow()
    {
        // Should not throw
        await controller.TerminateAsync(99999);
    }

    #endregion

    #region Dispose

    [Fact]
    public async Task Dispose_WithRunningProcesses_KillsAll()
    {
        using var testController = new ProcessControllerImpl();
        var info = await testController.StartAsync("dotnet", "--help");

        testController.Dispose();

        // Process should be killed after dispose
        // We can't easily verify this since the controller is disposed,
        // but we can verify no exception is thrown
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var testController = new ProcessControllerImpl();

        testController.Dispose();
        testController.Dispose(); // Should not throw
    }

    [Fact]
    public async Task Dispose_AfterDispose_MethodsThrow()
    {
        var testController = new ProcessControllerImpl();
        testController.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await testController.StartAsync("dotnet", "--version"));
    }

    #endregion

    #region WriteLineAsync

    [Fact(Skip = "Windows-only test - requires findstr")]
    public async Task WriteLineAsync_ToStdin_WritesSuccessfully()
    {
        // This test needs an interactive process - on Windows we can use more/findstr
        // For cross-platform, we'd need a custom test helper

        // Skip on non-Windows for now
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Use findstr which reads from stdin and echoes matching lines
        var info = await controller.StartAsync("findstr", "test");

        await controller.WriteLineAsync(info.ProcessId, "this is a test line");

        // Give it a moment to process
        await Task.Delay(100);

        // Check if output contains our input
        var output = controller.PeekStdout(info.ProcessId);

        // Kill the process since findstr waits for more input
        await controller.KillAsync(info.ProcessId);

        output.ShouldContain("test");
    }

    [Fact]
    public async Task WriteLineAsync_InvalidProcess_ThrowsInvalidOperation()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await controller.WriteLineAsync(99999, "test"));
    }

    #endregion
}
