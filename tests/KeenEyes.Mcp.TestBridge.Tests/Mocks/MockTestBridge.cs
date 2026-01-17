using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge;
using KeenEyes.Testing.Input;
using KeenEyes.TestBridge.AI;
using KeenEyes.TestBridge.Animation;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Commands;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.Logging;
using KeenEyes.TestBridge.Mutation;
using KeenEyes.TestBridge.Navigation;
using KeenEyes.TestBridge.Network;
using KeenEyes.TestBridge.Physics;
using KeenEyes.TestBridge.Process;
using KeenEyes.TestBridge.Profile;
using KeenEyes.TestBridge.Replay;
using KeenEyes.TestBridge.Snapshot;
using KeenEyes.TestBridge.State;
using KeenEyes.TestBridge.Systems;
using KeenEyes.TestBridge.Time;
using KeenEyes.TestBridge.UI;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ITestBridge for testing MCP tools.
/// </summary>
internal sealed class MockTestBridge : ITestBridge
{
    public bool IsConnected { get; set; } = true;

    // Core controllers
    public MockInputController MockInput { get; } = new();
    public MockStateController MockState { get; } = new();
    public MockCaptureController MockCapture { get; } = new();
    public MockProcessController MockProcess { get; } = new();
    public MockLogController MockLogs { get; } = new();

    // Extended controllers
    public MockWindowController MockWindow { get; } = new();
    public MockTimeController MockTime { get; } = new();
    public MockSystemController MockSystems { get; } = new();
    public MockMutationController MockMutation { get; } = new();
    public MockProfileController MockProfile { get; } = new();
    public MockSnapshotController MockSnapshot { get; } = new();
    public MockAIController MockAI { get; } = new();
    public MockReplayController MockReplay { get; } = new();
    public MockPhysicsController MockPhysics { get; } = new();
    public MockAnimationController MockAnimation { get; } = new();
    public MockNavigationController MockNavigation { get; } = new();
    public MockNetworkController MockNetwork { get; } = new();
    public MockUIController MockUI { get; } = new();
    public KeenEyes.Testing.Input.MockInputContext MockInputContext { get; } = new();

    // Core interface implementations
    IInputController ITestBridge.Input => MockInput;
    ICaptureController ITestBridge.Capture => MockCapture;
    IStateController ITestBridge.State => MockState;
    IProcessController ITestBridge.Process => MockProcess;
    ILogController ITestBridge.Logs => MockLogs;

    // Extended interface implementations
    IWindowController ITestBridge.Window => MockWindow;
    ITimeController ITestBridge.Time => MockTime;
    ISystemController ITestBridge.Systems => MockSystems;
    IMutationController ITestBridge.Mutation => MockMutation;
    IProfileController ITestBridge.Profile => MockProfile;
    ISnapshotController ITestBridge.Snapshot => MockSnapshot;
    IAIController ITestBridge.AI => MockAI;
    IReplayController ITestBridge.Replay => MockReplay;
    IPhysicsController ITestBridge.Physics => MockPhysics;
    IAnimationController ITestBridge.Animation => MockAnimation;
    INavigationController ITestBridge.Navigation => MockNavigation;
    INetworkController ITestBridge.Network => MockNetwork;
    IUIController ITestBridge.UI => MockUI;
    IInputContext ITestBridge.InputContext => MockInputContext;

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
