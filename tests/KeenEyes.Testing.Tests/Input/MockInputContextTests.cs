using System.Numerics;
using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class MockInputContextTests
{
    #region Constructor

    [Fact]
    public void Constructor_DefaultGamepadCount_CreatesFourGamepads()
    {
        using var context = new MockInputContext();

        Assert.Equal(4, context.Gamepads.Length);
    }

    [Fact]
    public void Constructor_CustomGamepadCount_CreatesSpecifiedGamepads()
    {
        using var context = new MockInputContext(gamepadCount: 2);

        Assert.Equal(2, context.Gamepads.Length);
    }

    [Fact]
    public void Constructor_FirstGamepadConnected_OthersDisconnected()
    {
        using var context = new MockInputContext(gamepadCount: 4);

        Assert.True(context.GetMockGamepad(0).IsConnected);
        Assert.False(context.GetMockGamepad(1).IsConnected);
        Assert.False(context.GetMockGamepad(2).IsConnected);
        Assert.False(context.GetMockGamepad(3).IsConnected);
    }

    #endregion

    #region Device Access

    [Fact]
    public void MockKeyboard_ReturnsKeyboard()
    {
        using var context = new MockInputContext();

        Assert.NotNull(context.MockKeyboard);
        Assert.IsType<MockKeyboard>(context.MockKeyboard);
    }

    [Fact]
    public void MockMouse_ReturnsMouse()
    {
        using var context = new MockInputContext();

        Assert.NotNull(context.MockMouse);
        Assert.IsType<MockMouse>(context.MockMouse);
    }

    [Fact]
    public void GetMockGamepad_ValidIndex_ReturnsGamepad()
    {
        using var context = new MockInputContext(gamepadCount: 2);

        var gamepad = context.GetMockGamepad(1);

        Assert.NotNull(gamepad);
        Assert.Equal(1, gamepad.Index);
    }

    [Fact]
    public void GetMockGamepad_NegativeIndex_Throws()
    {
        using var context = new MockInputContext();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => context.GetMockGamepad(-1));
        Assert.Contains("0", ex.Message);
    }

    [Fact]
    public void GetMockGamepad_IndexOutOfRange_Throws()
    {
        using var context = new MockInputContext(gamepadCount: 2);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => context.GetMockGamepad(5));
        Assert.Contains("0", ex.Message);
        Assert.Contains("1", ex.Message);
    }

    #endregion

    #region IInputContext Properties

    [Fact]
    public void Keyboard_ReturnsMockKeyboard()
    {
        using var context = new MockInputContext();

        Assert.Same(context.MockKeyboard, context.Keyboard);
    }

    [Fact]
    public void Mouse_ReturnsMockMouse()
    {
        using var context = new MockInputContext();

        Assert.Same(context.MockMouse, context.Mouse);
    }

    [Fact]
    public void Gamepad_ReturnsFirstGamepad()
    {
        using var context = new MockInputContext();

        Assert.Same(context.GetMockGamepad(0), context.Gamepad);
    }

    [Fact]
    public void Keyboards_ReturnsSingleKeyboard()
    {
        using var context = new MockInputContext();

        Assert.Single(context.Keyboards);
        Assert.Same(context.MockKeyboard, context.Keyboards[0]);
    }

    [Fact]
    public void Mice_ReturnsSingleMouse()
    {
        using var context = new MockInputContext();

        Assert.Single(context.Mice);
        Assert.Same(context.MockMouse, context.Mice[0]);
    }

    [Fact]
    public void Gamepads_ReturnsAllGamepads()
    {
        using var context = new MockInputContext(gamepadCount: 3);

        Assert.Equal(3, context.Gamepads.Length);
    }

    [Fact]
    public void ConnectedGamepadCount_ReturnsCorrectCount()
    {
        using var context = new MockInputContext(gamepadCount: 4);
        context.SetGamepadConnected(1, true);
        context.SetGamepadConnected(2, true);

        Assert.Equal(3, context.ConnectedGamepadCount); // 0, 1, 2 connected
    }

    #endregion

    #region Convenience Methods - Keyboard

    [Fact]
    public void SetKeyDown_SetsKeyViaKeyboard()
    {
        using var context = new MockInputContext();

        context.SetKeyDown(Key.W);

        Assert.True(context.MockKeyboard.IsKeyDown(Key.W));
    }

    [Fact]
    public void SetKeyUp_ReleasesKeyViaKeyboard()
    {
        using var context = new MockInputContext();
        context.SetKeyDown(Key.W);

        context.SetKeyUp(Key.W);

        Assert.False(context.MockKeyboard.IsKeyDown(Key.W));
    }

    [Fact]
    public void SimulateKeyDown_FiresEvent()
    {
        using var context = new MockInputContext();
        KeyEventArgs? receivedArgs = null;
        context.MockKeyboard.OnKeyDown += args => receivedArgs = args;

        context.SimulateKeyDown(Key.Space, KeyModifiers.Control);

        Assert.NotNull(receivedArgs);
        Assert.Equal(Key.Space, receivedArgs.Value.Key);
        Assert.Equal(KeyModifiers.Control, receivedArgs.Value.Modifiers);
    }

    [Fact]
    public void SimulateKeyUp_FiresEvent()
    {
        using var context = new MockInputContext();
        KeyEventArgs? receivedArgs = null;
        context.MockKeyboard.OnKeyUp += args => receivedArgs = args;

        context.SimulateKeyUp(Key.Space, KeyModifiers.Alt);

        Assert.NotNull(receivedArgs);
        Assert.Equal(Key.Space, receivedArgs.Value.Key);
        Assert.Equal(KeyModifiers.Alt, receivedArgs.Value.Modifiers);
    }

    #endregion

    #region Convenience Methods - Mouse

    [Fact]
    public void SetMousePosition_Floats_SetsPositionViaMouse()
    {
        using var context = new MockInputContext();

        context.SetMousePosition(100, 200);

        Assert.Equal(new Vector2(100, 200), context.MockMouse.Position);
    }

    [Fact]
    public void SetMousePosition_Vector2_SetsPositionViaMouse()
    {
        using var context = new MockInputContext();

        context.SetMousePosition(new Vector2(150, 250));

        Assert.Equal(new Vector2(150, 250), context.MockMouse.Position);
    }

    [Fact]
    public void SetMouseButton_Down_SetsButtonDown()
    {
        using var context = new MockInputContext();

        context.SetMouseButton(MouseButton.Left, true);

        Assert.True(context.MockMouse.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void SetMouseButton_Up_ReleasesButton()
    {
        using var context = new MockInputContext();
        context.SetMouseButton(MouseButton.Left, true);

        context.SetMouseButton(MouseButton.Left, false);

        Assert.False(context.MockMouse.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void SimulateMouseMove_FiresEvent()
    {
        using var context = new MockInputContext();
        MouseMoveEventArgs? receivedArgs = null;
        context.MockMouse.OnMove += args => receivedArgs = args;

        context.SimulateMouseMove(new Vector2(100, 200));

        Assert.NotNull(receivedArgs);
        Assert.Equal(new Vector2(100, 200), receivedArgs.Value.Position);
    }

    [Fact]
    public void SimulateMouseClick_FiresDownAndUpEvents()
    {
        using var context = new MockInputContext();
        var events = new List<string>();
        context.MockMouse.OnButtonDown += _ => events.Add("Down");
        context.MockMouse.OnButtonUp += _ => events.Add("Up");

        context.SimulateMouseClick(MouseButton.Left);

        Assert.Equal(["Down", "Up"], events);
    }

    #endregion

    #region Convenience Methods - Gamepad

    [Fact]
    public void SetGamepadButton_Down_SetsButtonDown()
    {
        using var context = new MockInputContext();

        context.SetGamepadButton(0, GamepadButton.South, true);

        Assert.True(context.GetMockGamepad(0).IsButtonDown(GamepadButton.South));
    }

    [Fact]
    public void SetGamepadButton_Up_ReleasesButton()
    {
        using var context = new MockInputContext();
        context.SetGamepadButton(0, GamepadButton.South, true);

        context.SetGamepadButton(0, GamepadButton.South, false);

        Assert.False(context.GetMockGamepad(0).IsButtonDown(GamepadButton.South));
    }

    [Fact]
    public void SetGamepadStick_Left_SetsLeftStick()
    {
        using var context = new MockInputContext();

        context.SetGamepadStick(0, isLeft: true, 0.5f, -0.3f);

        Assert.Equal(new Vector2(0.5f, -0.3f), context.GetMockGamepad(0).LeftStick);
    }

    [Fact]
    public void SetGamepadStick_Right_SetsRightStick()
    {
        using var context = new MockInputContext();

        context.SetGamepadStick(0, isLeft: false, 0.7f, 0.2f);

        Assert.Equal(new Vector2(0.7f, 0.2f), context.GetMockGamepad(0).RightStick);
    }

    [Fact]
    public void SetGamepadTrigger_Left_SetsLeftTrigger()
    {
        using var context = new MockInputContext();

        context.SetGamepadTrigger(0, isLeft: true, 0.8f);

        Assert.Equal(0.8f, context.GetMockGamepad(0).LeftTrigger);
    }

    [Fact]
    public void SetGamepadTrigger_Right_SetsRightTrigger()
    {
        using var context = new MockInputContext();

        context.SetGamepadTrigger(0, isLeft: false, 0.4f);

        Assert.Equal(0.4f, context.GetMockGamepad(0).RightTrigger);
    }

    [Fact]
    public void SetGamepadConnected_SetsConnectionState()
    {
        using var context = new MockInputContext();

        context.SetGamepadConnected(1, true);

        Assert.True(context.GetMockGamepad(1).IsConnected);
    }

    #endregion

    #region Global Events

    [Fact]
    public void OnKeyDown_FiredWhenKeyboardSimulatesKeyDown()
    {
        using var context = new MockInputContext();
        KeyEventArgs? receivedArgs = null;
        IKeyboard? receivedKeyboard = null;
        context.OnKeyDown += (keyboard, args) =>
        {
            receivedKeyboard = keyboard;
            receivedArgs = args;
        };

        context.MockKeyboard.SimulateKeyDown(Key.A);

        Assert.Same(context.MockKeyboard, receivedKeyboard);
        Assert.Equal(Key.A, receivedArgs!.Value.Key);
    }

    [Fact]
    public void OnKeyUp_FiredWhenKeyboardSimulatesKeyUp()
    {
        using var context = new MockInputContext();
        KeyEventArgs? receivedArgs = null;
        context.OnKeyUp += (_, args) => receivedArgs = args;

        context.MockKeyboard.SimulateKeyUp(Key.B);

        Assert.Equal(Key.B, receivedArgs!.Value.Key);
    }

    [Fact]
    public void OnTextInput_FiredWhenKeyboardSimulatesTextInput()
    {
        using var context = new MockInputContext();
        char? receivedChar = null;
        context.OnTextInput += (_, c) => receivedChar = c;

        context.MockKeyboard.SimulateTextInput('X');

        Assert.Equal('X', receivedChar);
    }

    [Fact]
    public void OnMouseButtonDown_FiredWhenMouseSimulatesButtonDown()
    {
        using var context = new MockInputContext();
        MouseButtonEventArgs? receivedArgs = null;
        context.OnMouseButtonDown += (_, args) => receivedArgs = args;

        context.MockMouse.SimulateButtonDown(MouseButton.Right);

        Assert.Equal(MouseButton.Right, receivedArgs!.Value.Button);
    }

    [Fact]
    public void OnMouseButtonUp_FiredWhenMouseSimulatesButtonUp()
    {
        using var context = new MockInputContext();
        MouseButtonEventArgs? receivedArgs = null;
        context.OnMouseButtonUp += (_, args) => receivedArgs = args;

        context.MockMouse.SimulateButtonUp(MouseButton.Middle);

        Assert.Equal(MouseButton.Middle, receivedArgs!.Value.Button);
    }

    [Fact]
    public void OnMouseMove_FiredWhenMouseSimulatesMove()
    {
        using var context = new MockInputContext();
        MouseMoveEventArgs? receivedArgs = null;
        context.OnMouseMove += (_, args) => receivedArgs = args;

        context.MockMouse.SimulateMove(new Vector2(50, 75));

        Assert.Equal(new Vector2(50, 75), receivedArgs!.Value.Position);
    }

    [Fact]
    public void OnMouseScroll_FiredWhenMouseSimulatesScroll()
    {
        using var context = new MockInputContext();
        MouseScrollEventArgs? receivedArgs = null;
        context.OnMouseScroll += (_, args) => receivedArgs = args;

        context.MockMouse.SimulateScroll(0, 120);

        Assert.Equal(new Vector2(0, 120), receivedArgs!.Value.Delta);
    }

    [Fact]
    public void OnGamepadButtonDown_FiredWhenGamepadSimulatesButtonDown()
    {
        using var context = new MockInputContext();
        GamepadButtonEventArgs? receivedArgs = null;
        context.OnGamepadButtonDown += (_, args) => receivedArgs = args;

        context.GetMockGamepad(0).SimulateButtonDown(GamepadButton.North);

        Assert.Equal(GamepadButton.North, receivedArgs!.Value.Button);
    }

    [Fact]
    public void OnGamepadButtonUp_FiredWhenGamepadSimulatesButtonUp()
    {
        using var context = new MockInputContext();
        GamepadButtonEventArgs? receivedArgs = null;
        context.OnGamepadButtonUp += (_, args) => receivedArgs = args;

        context.GetMockGamepad(0).SimulateButtonUp(GamepadButton.West);

        Assert.Equal(GamepadButton.West, receivedArgs!.Value.Button);
    }

    [Fact]
    public void OnGamepadConnected_FiredWhenGamepadSimulatesConnect()
    {
        using var context = new MockInputContext();
        IGamepad? receivedGamepad = null;
        context.OnGamepadConnected += g => receivedGamepad = g;

        context.GetMockGamepad(1).SimulateConnect();

        Assert.Same(context.GetMockGamepad(1), receivedGamepad);
    }

    [Fact]
    public void OnGamepadDisconnected_FiredWhenGamepadSimulatesDisconnect()
    {
        using var context = new MockInputContext();
        IGamepad? receivedGamepad = null;
        context.OnGamepadDisconnected += g => receivedGamepad = g;

        context.GetMockGamepad(0).SimulateDisconnect();

        Assert.Same(context.GetMockGamepad(0), receivedGamepad);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_DoesNothing()
    {
        using var context = new MockInputContext();
        context.SetKeyDown(Key.W);

        context.Update(); // Should not throw or change state

        Assert.True(context.MockKeyboard.IsKeyDown(Key.W));
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var context = new MockInputContext();

        context.Dispose();
        context.Dispose(); // Should not throw
    }

    #endregion
}
