using KeenEyes.TestBridge;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Commands;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.Logging;
using KeenEyes.TestBridge.Process;
using KeenEyes.TestBridge.State;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ITestBridge for testing MCP tools.
/// </summary>
internal sealed class MockTestBridge : ITestBridge
{
    public bool IsConnected { get; set; } = true;
    public MockInputController MockInput { get; } = new();
    public MockStateController MockState { get; } = new();
    public MockCaptureController MockCapture { get; } = new();
    public MockProcessController MockProcess { get; } = new();
    public MockLogController MockLogs { get; } = new();

    IInputController ITestBridge.Input => MockInput;
    ICaptureController ITestBridge.Capture => MockCapture;
    IStateController ITestBridge.State => MockState;
    IProcessController ITestBridge.Process => MockProcess;
    ILogController ITestBridge.Logs => MockLogs;

    public Task<CommandResult> ExecuteAsync(ITestCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CommandResult.Ok());
    }

    public Task<bool> WaitForAsync(Func<IStateController, Task<bool>> condition, TimeSpan timeout, TimeSpan? pollInterval = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> WaitForAsync(Func<IStateController, bool> condition, TimeSpan timeout, TimeSpan? pollInterval = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
