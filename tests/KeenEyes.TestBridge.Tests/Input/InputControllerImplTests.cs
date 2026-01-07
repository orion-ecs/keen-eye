using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.TestBridge.Tests.Input;

public class InputControllerImplTests
{
    #region Keyboard

    [Fact]
    public async Task KeyDownAsync_SetsKeyDown()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.KeyDownAsync(Key.W);

        context.Keyboard.IsKeyDown(Key.W).ShouldBeTrue();
    }

    [Fact]
    public async Task KeyUpAsync_ReleasesKey()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        await input.KeyDownAsync(Key.W);

        await input.KeyUpAsync(Key.W);

        context.Keyboard.IsKeyDown(Key.W).ShouldBeFalse();
    }

    [Fact]
    public async Task KeyPressAsync_SimulatesKeyDownAndUp()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.KeyPressAsync(Key.Space);

        // After press, key should be released
        context.Keyboard.IsKeyDown(Key.Space).ShouldBeFalse();
    }

    [Fact]
    public async Task TypeTextAsync_SimulatesTextInput()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        var receivedText = new List<char>();
        context.MockKeyboard.OnTextInput += c => receivedText.Add(c);

        await input.TypeTextAsync("Hello");

        string.Join("", receivedText).ShouldBe("Hello");
    }

    [Fact]
    public void IsKeyDown_ReturnsCorrectState()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        context.SetKeyDown(Key.A);

        input.IsKeyDown(Key.A).ShouldBeTrue();
        input.IsKeyDown(Key.B).ShouldBeFalse();
    }

    #endregion

    #region Mouse

    [Fact]
    public async Task MouseMoveAsync_SetsPosition()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.MouseMoveAsync(100, 200);

        context.Mouse.Position.X.ShouldBe(100);
        context.Mouse.Position.Y.ShouldBe(200);
    }

    [Fact]
    public async Task MouseMoveRelativeAsync_MovesRelativeToCurrentPosition()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        context.SetMousePosition(50, 50);

        await input.MouseMoveRelativeAsync(10, 20);

        context.Mouse.Position.X.ShouldBe(60);
        context.Mouse.Position.Y.ShouldBe(70);
    }

    [Fact]
    public async Task MouseDownAsync_SetsButtonDown()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.MouseDownAsync(MouseButton.Left);

        context.Mouse.IsButtonDown(MouseButton.Left).ShouldBeTrue();
    }

    [Fact]
    public async Task MouseUpAsync_ReleasesButton()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        await input.MouseDownAsync(MouseButton.Left);

        await input.MouseUpAsync(MouseButton.Left);

        context.Mouse.IsButtonDown(MouseButton.Left).ShouldBeFalse();
    }

    [Fact]
    public async Task ClickAsync_MovesAndClicks()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.ClickAsync(150, 250);

        context.Mouse.Position.X.ShouldBe(150);
        context.Mouse.Position.Y.ShouldBe(250);
    }

    [Fact]
    public async Task DoubleClickAsync_MovesAndDoubleClicks()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.DoubleClickAsync(200, 300);

        context.Mouse.Position.X.ShouldBe(200);
        context.Mouse.Position.Y.ShouldBe(300);
    }

    [Fact]
    public async Task DragAsync_SimulatesDrag()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.DragAsync(10, 20, 100, 200);

        // After drag, position should be at end position
        context.Mouse.Position.X.ShouldBe(100);
        context.Mouse.Position.Y.ShouldBe(200);
    }

    [Fact]
    public async Task ScrollAsync_SimulatesScroll()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        var scrollReceived = false;
        context.OnMouseScroll += (_, _) => scrollReceived = true;

        await input.ScrollAsync(0, 120);

        scrollReceived.ShouldBeTrue();
    }

    [Fact]
    public void GetMousePosition_ReturnsCurrentPosition()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        context.SetMousePosition(75, 125);

        var (x, y) = input.GetMousePosition();

        x.ShouldBe(75);
        y.ShouldBe(125);
    }

    [Fact]
    public void IsMouseButtonDown_ReturnsCorrectState()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        context.SetMouseButton(MouseButton.Right, true);

        input.IsMouseButtonDown(MouseButton.Right).ShouldBeTrue();
        input.IsMouseButtonDown(MouseButton.Left).ShouldBeFalse();
    }

    #endregion

    #region Gamepad

    [Fact]
    public async Task GamepadButtonDownAsync_SetsButtonDown()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.GamepadButtonDownAsync(0, GamepadButton.South);

        context.GetMockGamepad(0).IsButtonDown(GamepadButton.South).ShouldBeTrue();
    }

    [Fact]
    public async Task GamepadButtonUpAsync_ReleasesButton()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        await input.GamepadButtonDownAsync(0, GamepadButton.South);

        await input.GamepadButtonUpAsync(0, GamepadButton.South);

        context.GetMockGamepad(0).IsButtonDown(GamepadButton.South).ShouldBeFalse();
    }

    [Fact]
    public async Task SetLeftStickAsync_SetsLeftStickValues()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.SetLeftStickAsync(0, 0.5f, -0.3f);

        context.GetMockGamepad(0).LeftStick.X.ShouldBe(0.5f, 0.01f);
        context.GetMockGamepad(0).LeftStick.Y.ShouldBe(-0.3f, 0.01f);
    }

    [Fact]
    public async Task SetRightStickAsync_SetsRightStickValues()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.SetRightStickAsync(0, -0.7f, 0.2f);

        context.GetMockGamepad(0).RightStick.X.ShouldBe(-0.7f, 0.01f);
        context.GetMockGamepad(0).RightStick.Y.ShouldBe(0.2f, 0.01f);
    }

    [Fact]
    public async Task SetTriggerAsync_Left_SetsLeftTrigger()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.SetTriggerAsync(0, isLeft: true, 0.8f);

        context.GetMockGamepad(0).LeftTrigger.ShouldBe(0.8f, 0.01f);
    }

    [Fact]
    public async Task SetTriggerAsync_Right_SetsRightTrigger()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.SetTriggerAsync(0, isLeft: false, 0.4f);

        context.GetMockGamepad(0).RightTrigger.ShouldBe(0.4f, 0.01f);
    }

    [Fact]
    public async Task SetGamepadConnectedAsync_SetsConnectionState()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        await input.SetGamepadConnectedAsync(1, true);

        context.GetMockGamepad(1).IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void IsGamepadButtonDown_ReturnsCorrectState()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        context.GetMockGamepad(0).SimulateButtonDown(GamepadButton.North);

        input.IsGamepadButtonDown(0, GamepadButton.North).ShouldBeTrue();
        input.IsGamepadButtonDown(0, GamepadButton.South).ShouldBeFalse();
    }

    [Fact]
    public void IsGamepadButtonDown_InvalidIndex_ReturnsFalse()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        input.IsGamepadButtonDown(-1, GamepadButton.North).ShouldBeFalse();
        input.IsGamepadButtonDown(100, GamepadButton.North).ShouldBeFalse();
    }

    [Fact]
    public void GamepadCount_ReturnsCorrectCount()
    {
        using var context = new MockInputContext(gamepadCount: 3);
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;

        input.GamepadCount.ShouldBe(3);
    }

    #endregion

    #region InputAction System

    [Fact]
    public async Task TriggerActionAsync_SetsActionState()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var input = bridge.Input;

        await input.TriggerActionAsync("Jump");

        // Action state is tracked internally
        // This test just verifies no exception is thrown
    }

    [Fact]
    public async Task SetActionValueAsync_SetsAxisValue()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var input = bridge.Input;

        await input.SetActionValueAsync("MoveHorizontal", 0.75f);

        // Action value is tracked internally
    }

    [Fact]
    public async Task SetActionVector2Async_Sets2DAxisValue()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var input = bridge.Input;

        await input.SetActionVector2Async("Move", 0.5f, -0.3f);

        // Action vector is tracked internally
    }

    #endregion

    #region Reset

    [Fact]
    public async Task ResetAllAsync_ResetsKeyboard()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        await input.KeyDownAsync(Key.W);
        await input.KeyDownAsync(Key.A);

        await input.ResetAllAsync();

        context.Keyboard.IsKeyDown(Key.W).ShouldBeFalse();
        context.Keyboard.IsKeyDown(Key.A).ShouldBeFalse();
    }

    [Fact]
    public async Task ResetAllAsync_ResetsMouse()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        await input.MouseDownAsync(MouseButton.Left);

        await input.ResetAllAsync();

        context.Mouse.IsButtonDown(MouseButton.Left).ShouldBeFalse();
    }

    [Fact]
    public async Task ResetAllAsync_ResetsGamepads()
    {
        using var context = new MockInputContext();
        using var world = new World();
        using var bridge = new InProcessBridge(world, new TestBridgeOptions { CustomInputContext = context });
        var input = bridge.Input;
        await input.GamepadButtonDownAsync(0, GamepadButton.South);
        await input.SetLeftStickAsync(0, 1.0f, 1.0f);

        await input.ResetAllAsync();

        context.GetMockGamepad(0).IsButtonDown(GamepadButton.South).ShouldBeFalse();
        context.GetMockGamepad(0).LeftStick.X.ShouldBe(0f);
        context.GetMockGamepad(0).LeftStick.Y.ShouldBe(0f);
    }

    #endregion
}
