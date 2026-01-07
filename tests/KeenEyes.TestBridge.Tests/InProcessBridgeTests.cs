using KeenEyes.TestBridge.Commands;

namespace KeenEyes.TestBridge.Tests;

public class InProcessBridgeTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithWorld_CreatesConnectedBridge()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithWorld_ExposesWorld()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.World.ShouldBeSameAs(world);
    }

    [Fact]
    public void Constructor_WithDefaultOptions_CreatesFourGamepads()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.InputContext.Gamepads.Length.ShouldBe(4);
    }

    [Fact]
    public void Constructor_WithOptions_UsesGamepadCount()
    {
        using var world = new World();
        var options = new TestBridgeOptions { GamepadCount = 2 };
        using var bridge = new InProcessBridge(world, options);

        bridge.InputContext.Gamepads.Length.ShouldBe(2);
    }

    #endregion

    #region Controllers

    [Fact]
    public void Input_ReturnsInputController()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.Input.ShouldNotBeNull();
    }

    [Fact]
    public void State_ReturnsStateController()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.State.ShouldNotBeNull();
    }

    [Fact]
    public void Capture_ReturnsCaptureController()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.Capture.ShouldNotBeNull();
    }

    #endregion

    #region WaitForAsync

    [Fact]
    public async Task WaitForAsync_ConditionMet_ReturnsTrue()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        world.Spawn().Build();

#pragma warning disable xUnit1051 // Test needs to verify default cancellation behavior
        var result = await bridge.WaitForAsync(
            state => state.GetEntityCountAsync().Result > 0,
            TimeSpan.FromSeconds(1));
#pragma warning restore xUnit1051

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitForAsync_ConditionNeverMet_ReturnsFalse()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

#pragma warning disable xUnit1051 // Test needs to verify timeout behavior without cancellation
        var result = await bridge.WaitForAsync(
            state => state.GetEntityCountAsync().Result > 0,
            TimeSpan.FromMilliseconds(50),
            pollInterval: TimeSpan.FromMilliseconds(10));
#pragma warning restore xUnit1051

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task WaitForAsync_Disposed_ReturnsFalse()
    {
        using var world = new World();
        var bridge = new InProcessBridge(world);
        bridge.Dispose();

#pragma warning disable xUnit1051 // Test needs to verify disposed behavior
        var result = await bridge.WaitForAsync(
            _ => true,
            TimeSpan.FromSeconds(1));
#pragma warning restore xUnit1051

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task WaitForAsync_Cancelled_ThrowsOperationCancelled()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await bridge.WaitForAsync(
                _ => false,
                TimeSpan.FromSeconds(10),
                cancellationToken: cts.Token));
    }

    [Fact]
    public async Task WaitForAsync_AsyncCondition_Works()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        world.Spawn().Build();

#pragma warning disable xUnit1051 // Test needs to verify async condition behavior
        var result = await bridge.WaitForAsync(
            async state =>
            {
                var count = await state.GetEntityCountAsync();
                return count > 0;
            },
            TimeSpan.FromSeconds(1));
#pragma warning restore xUnit1051

        result.ShouldBeTrue();
    }

    #endregion

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ReturnsFailed()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var command = new TestCommand();

#pragma warning disable xUnit1051 // Test needs to verify command execution without cancellation
        var result = await bridge.ExecuteAsync(command);
#pragma warning restore xUnit1051

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.ShouldContain("Unknown");
    }

    [Fact]
    public async Task ExecuteAsync_Disposed_ReturnsFailed()
    {
        using var world = new World();
        var bridge = new InProcessBridge(world);
        bridge.Dispose();
        var command = new TestCommand();

#pragma warning disable xUnit1051 // Test needs to verify disposed behavior
        var result = await bridge.ExecuteAsync(command);
#pragma warning restore xUnit1051

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.ShouldContain("disposed");
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_SetsIsConnectedToFalse()
    {
        using var world = new World();
        var bridge = new InProcessBridge(world);

        bridge.Dispose();

        bridge.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var world = new World();
        var bridge = new InProcessBridge(world);

        Should.NotThrow(() =>
        {
            bridge.Dispose();
            bridge.Dispose();
        });
    }

    #endregion

    private sealed class TestCommand : ITestCommand
    {
        public string CommandType => "test";
    }
}
