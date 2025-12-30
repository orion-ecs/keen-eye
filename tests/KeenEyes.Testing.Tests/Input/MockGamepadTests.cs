using System.Numerics;
using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class MockGamepadTests
{
    #region Constructor

    [Fact]
    public void Constructor_DefaultParameters_SetsDefaults()
    {
        var gamepad = new MockGamepad();

        Assert.Equal(0, gamepad.Index);
        Assert.Equal("Mock Gamepad", gamepad.Name);
        Assert.True(gamepad.IsConnected);
    }

    [Fact]
    public void Constructor_WithParameters_SetsValues()
    {
        var gamepad = new MockGamepad(index: 2, name: "Test Pad", connected: false);

        Assert.Equal(2, gamepad.Index);
        Assert.Equal("Test Pad", gamepad.Name);
        Assert.False(gamepad.IsConnected);
    }

    #endregion

    #region SetConnected

    [Fact]
    public void SetConnected_True_SetsIsConnected()
    {
        var gamepad = new MockGamepad(connected: false);

        gamepad.SetConnected(true);

        Assert.True(gamepad.IsConnected);
    }

    [Fact]
    public void SetConnected_False_ClearsIsConnected()
    {
        var gamepad = new MockGamepad(connected: true);

        gamepad.SetConnected(false);

        Assert.False(gamepad.IsConnected);
    }

    #endregion

    #region SetButtonDown/SetButtonUp

    [Fact]
    public void SetButtonDown_SetsButtonPressed()
    {
        var gamepad = new MockGamepad();

        gamepad.SetButtonDown(GamepadButton.South);

        Assert.True(gamepad.IsButtonDown(GamepadButton.South));
    }

    [Fact]
    public void SetButtonUp_ReleasesButton()
    {
        var gamepad = new MockGamepad();
        gamepad.SetButtonDown(GamepadButton.South);

        gamepad.SetButtonUp(GamepadButton.South);

        Assert.False(gamepad.IsButtonDown(GamepadButton.South));
    }

    [Fact]
    public void IsButtonUp_WhenNotPressed_ReturnsTrue()
    {
        var gamepad = new MockGamepad();

        Assert.True(gamepad.IsButtonUp(GamepadButton.South));
    }

    [Fact]
    public void SetButtonDown_AllButtons_TracksAllButtons()
    {
        var gamepad = new MockGamepad();
        var buttons = new[]
        {
            GamepadButton.South, GamepadButton.East, GamepadButton.West, GamepadButton.North,
            GamepadButton.LeftShoulder, GamepadButton.RightShoulder,
            GamepadButton.LeftTrigger, GamepadButton.RightTrigger,
            GamepadButton.DPadUp, GamepadButton.DPadDown, GamepadButton.DPadLeft, GamepadButton.DPadRight,
            GamepadButton.LeftStick, GamepadButton.RightStick,
            GamepadButton.Start, GamepadButton.Back, GamepadButton.Guide
        };

        foreach (var button in buttons)
        {
            gamepad.SetButtonDown(button);
        }

        foreach (var button in buttons)
        {
            Assert.True(gamepad.IsButtonDown(button), $"Button {button} should be down");
        }
    }

    #endregion

    #region SetLeftStick/SetRightStick

    [Fact]
    public void SetLeftStick_SetsPosition()
    {
        var gamepad = new MockGamepad();

        gamepad.SetLeftStick(0.5f, -0.5f);

        Assert.Equal(new Vector2(0.5f, -0.5f), gamepad.LeftStick);
    }

    [Fact]
    public void SetLeftStick_ClampsValues()
    {
        var gamepad = new MockGamepad();

        gamepad.SetLeftStick(2.0f, -2.0f);

        Assert.Equal(new Vector2(1.0f, -1.0f), gamepad.LeftStick);
    }

    [Fact]
    public void SetRightStick_SetsPosition()
    {
        var gamepad = new MockGamepad();

        gamepad.SetRightStick(0.7f, 0.3f);

        Assert.Equal(new Vector2(0.7f, 0.3f), gamepad.RightStick);
    }

    [Fact]
    public void SetRightStick_ClampsValues()
    {
        var gamepad = new MockGamepad();

        gamepad.SetRightStick(-5.0f, 5.0f);

        Assert.Equal(new Vector2(-1.0f, 1.0f), gamepad.RightStick);
    }

    #endregion

    #region SetLeftTrigger/SetRightTrigger

    [Fact]
    public void SetLeftTrigger_SetsValue()
    {
        var gamepad = new MockGamepad();

        gamepad.SetLeftTrigger(0.75f);

        Assert.Equal(0.75f, gamepad.LeftTrigger);
    }

    [Fact]
    public void SetLeftTrigger_ClampsValues()
    {
        var gamepad = new MockGamepad();

        gamepad.SetLeftTrigger(1.5f);

        Assert.Equal(1.0f, gamepad.LeftTrigger);
    }

    [Fact]
    public void SetLeftTrigger_ClampsNegativeValues()
    {
        var gamepad = new MockGamepad();

        gamepad.SetLeftTrigger(-0.5f);

        Assert.Equal(0.0f, gamepad.LeftTrigger);
    }

    [Fact]
    public void SetRightTrigger_SetsValue()
    {
        var gamepad = new MockGamepad();

        gamepad.SetRightTrigger(0.25f);

        Assert.Equal(0.25f, gamepad.RightTrigger);
    }

    [Fact]
    public void SetRightTrigger_ClampsValues()
    {
        var gamepad = new MockGamepad();

        gamepad.SetRightTrigger(2.0f);

        Assert.Equal(1.0f, gamepad.RightTrigger);
    }

    #endregion

    #region ClearAllInputs

    [Fact]
    public void ClearAllInputs_ResetsAllState()
    {
        var gamepad = new MockGamepad();
        gamepad.SetButtonDown(GamepadButton.South);
        gamepad.SetLeftStick(1.0f, 1.0f);
        gamepad.SetRightStick(-1.0f, -1.0f);
        gamepad.SetLeftTrigger(1.0f);
        gamepad.SetRightTrigger(1.0f);
        gamepad.SetVibration(0.5f, 0.5f);

        gamepad.ClearAllInputs();

        Assert.False(gamepad.IsButtonDown(GamepadButton.South));
        Assert.Equal(Vector2.Zero, gamepad.LeftStick);
        Assert.Equal(Vector2.Zero, gamepad.RightStick);
        Assert.Equal(0f, gamepad.LeftTrigger);
        Assert.Equal(0f, gamepad.RightTrigger);
        Assert.Equal((0f, 0f), gamepad.LastVibration);
    }

    #endregion

    #region SimulateButtonDown/SimulateButtonUp

    [Fact]
    public void SimulateButtonDown_SetsButtonAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        GamepadButtonEventArgs? receivedArgs = null;
        gamepad.OnButtonDown += args => receivedArgs = args;

        gamepad.SimulateButtonDown(GamepadButton.South);

        Assert.True(gamepad.IsButtonDown(GamepadButton.South));
        Assert.NotNull(receivedArgs);
        Assert.Equal(GamepadButton.South, receivedArgs.Value.Button);
        Assert.Equal(0, receivedArgs.Value.GamepadIndex);
    }

    [Fact]
    public void SimulateButtonUp_ReleasesButtonAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        gamepad.SimulateButtonDown(GamepadButton.South);
        GamepadButtonEventArgs? receivedArgs = null;
        gamepad.OnButtonUp += args => receivedArgs = args;

        gamepad.SimulateButtonUp(GamepadButton.South);

        Assert.False(gamepad.IsButtonDown(GamepadButton.South));
        Assert.NotNull(receivedArgs);
        Assert.Equal(GamepadButton.South, receivedArgs.Value.Button);
    }

    #endregion

    #region SimulateAxisChange

    [Fact]
    public void SimulateAxisChange_LeftStickX_UpdatesAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        GamepadAxisEventArgs? receivedArgs = null;
        gamepad.OnAxisChanged += args => receivedArgs = args;

        gamepad.SimulateAxisChange(GamepadAxis.LeftStickX, 0.75f);

        Assert.Equal(0.75f, gamepad.LeftStick.X);
        Assert.NotNull(receivedArgs);
        Assert.Equal(GamepadAxis.LeftStickX, receivedArgs.Value.Axis);
        Assert.Equal(0.75f, receivedArgs.Value.Value);
        Assert.Equal(0f, receivedArgs.Value.PreviousValue);
    }

    [Fact]
    public void SimulateAxisChange_LeftStickY_UpdatesAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.LeftStickY, -0.5f);

        Assert.Equal(-0.5f, gamepad.LeftStick.Y);
    }

    [Fact]
    public void SimulateAxisChange_RightStickX_UpdatesAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.RightStickX, 1.0f);

        Assert.Equal(1.0f, gamepad.RightStick.X);
    }

    [Fact]
    public void SimulateAxisChange_RightStickY_UpdatesAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.RightStickY, -1.0f);

        Assert.Equal(-1.0f, gamepad.RightStick.Y);
    }

    [Fact]
    public void SimulateAxisChange_LeftTrigger_UpdatesAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.LeftTrigger, 0.8f);

        Assert.Equal(0.8f, gamepad.LeftTrigger);
    }

    [Fact]
    public void SimulateAxisChange_RightTrigger_UpdatesAndFiresEvent()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.RightTrigger, 0.6f);

        Assert.Equal(0.6f, gamepad.RightTrigger);
    }

    [Fact]
    public void SimulateAxisChange_ClampsStickValues()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.LeftStickX, 5.0f);

        Assert.Equal(1.0f, gamepad.LeftStick.X);
    }

    [Fact]
    public void SimulateAxisChange_ClampsTriggerValues()
    {
        var gamepad = new MockGamepad();
        gamepad.OnAxisChanged += _ => { };

        gamepad.SimulateAxisChange(GamepadAxis.LeftTrigger, -0.5f);

        Assert.Equal(0f, gamepad.LeftTrigger);
    }

    #endregion

    #region SimulateConnect/SimulateDisconnect

    [Fact]
    public void SimulateConnect_SetsConnectedAndFiresEvent()
    {
        var gamepad = new MockGamepad(connected: false);
        IGamepad? receivedGamepad = null;
        gamepad.OnConnected += g => receivedGamepad = g;

        gamepad.SimulateConnect();

        Assert.True(gamepad.IsConnected);
        Assert.Same(gamepad, receivedGamepad);
    }

    [Fact]
    public void SimulateDisconnect_ClearsConnectionAndInputsAndFiresEvent()
    {
        var gamepad = new MockGamepad(connected: true);
        gamepad.SetButtonDown(GamepadButton.South);
        gamepad.SetLeftStick(1.0f, 0.5f);
        IGamepad? receivedGamepad = null;
        gamepad.OnDisconnected += g => receivedGamepad = g;

        gamepad.SimulateDisconnect();

        Assert.False(gamepad.IsConnected);
        Assert.False(gamepad.IsButtonDown(GamepadButton.South));
        Assert.Equal(Vector2.Zero, gamepad.LeftStick);
        Assert.Same(gamepad, receivedGamepad);
    }

    #endregion

    #region Vibration

    [Fact]
    public void SetVibration_SetsLastVibration()
    {
        var gamepad = new MockGamepad();

        gamepad.SetVibration(0.5f, 0.8f);

        Assert.Equal((0.5f, 0.8f), gamepad.LastVibration);
    }

    [Fact]
    public void SetVibration_ClampsValues()
    {
        var gamepad = new MockGamepad();

        gamepad.SetVibration(2.0f, -1.0f);

        Assert.Equal((1.0f, 0.0f), gamepad.LastVibration);
    }

    [Fact]
    public void IsVibrating_WhenVibrating_ReturnsTrue()
    {
        var gamepad = new MockGamepad();

        gamepad.SetVibration(0.1f, 0f);

        Assert.True(gamepad.IsVibrating);
    }

    [Fact]
    public void IsVibrating_WhenNotVibrating_ReturnsFalse()
    {
        var gamepad = new MockGamepad();

        Assert.False(gamepad.IsVibrating);
    }

    [Fact]
    public void StopVibration_ClearsVibration()
    {
        var gamepad = new MockGamepad();
        gamepad.SetVibration(1.0f, 1.0f);

        gamepad.StopVibration();

        Assert.Equal((0f, 0f), gamepad.LastVibration);
        Assert.False(gamepad.IsVibrating);
    }

    #endregion

    #region GetAxis

    [Fact]
    public void GetAxis_LeftStickX_ReturnsValue()
    {
        var gamepad = new MockGamepad();
        gamepad.SetLeftStick(0.5f, 0.3f);

        Assert.Equal(0.5f, gamepad.GetAxis(GamepadAxis.LeftStickX));
    }

    [Fact]
    public void GetAxis_LeftStickY_ReturnsValue()
    {
        var gamepad = new MockGamepad();
        gamepad.SetLeftStick(0.5f, 0.3f);

        Assert.Equal(0.3f, gamepad.GetAxis(GamepadAxis.LeftStickY));
    }

    [Fact]
    public void GetAxis_RightStickX_ReturnsValue()
    {
        var gamepad = new MockGamepad();
        gamepad.SetRightStick(0.7f, 0.2f);

        Assert.Equal(0.7f, gamepad.GetAxis(GamepadAxis.RightStickX));
    }

    [Fact]
    public void GetAxis_RightStickY_ReturnsValue()
    {
        var gamepad = new MockGamepad();
        gamepad.SetRightStick(0.7f, 0.2f);

        Assert.Equal(0.2f, gamepad.GetAxis(GamepadAxis.RightStickY));
    }

    [Fact]
    public void GetAxis_LeftTrigger_ReturnsValue()
    {
        var gamepad = new MockGamepad();
        gamepad.SetLeftTrigger(0.9f);

        Assert.Equal(0.9f, gamepad.GetAxis(GamepadAxis.LeftTrigger));
    }

    [Fact]
    public void GetAxis_RightTrigger_ReturnsValue()
    {
        var gamepad = new MockGamepad();
        gamepad.SetRightTrigger(0.4f);

        Assert.Equal(0.4f, gamepad.GetAxis(GamepadAxis.RightTrigger));
    }

    [Fact]
    public void GetAxis_UnknownAxis_ReturnsZero()
    {
        var gamepad = new MockGamepad();

        Assert.Equal(0f, gamepad.GetAxis((GamepadAxis)999));
    }

    #endregion

    #region GetState

    [Fact]
    public void GetState_ReturnsCurrentState()
    {
        var gamepad = new MockGamepad(index: 1);
        gamepad.SetButtonDown(GamepadButton.South);
        gamepad.SetButtonDown(GamepadButton.East);
        gamepad.SetLeftStick(0.5f, -0.5f);
        gamepad.SetRightStick(-0.3f, 0.7f);
        gamepad.SetLeftTrigger(0.8f);
        gamepad.SetRightTrigger(0.2f);

        var state = gamepad.GetState();

        Assert.Equal(1, state.Index);
        Assert.True(state.IsConnected);
        Assert.True(state.PressedButtons.HasFlag(GamepadButtons.South));
        Assert.True(state.PressedButtons.HasFlag(GamepadButtons.East));
        Assert.Equal(new Vector2(0.5f, -0.5f), state.LeftStick);
        Assert.Equal(new Vector2(-0.3f, 0.7f), state.RightStick);
        Assert.Equal(0.8f, state.LeftTrigger);
        Assert.Equal(0.2f, state.RightTrigger);
    }

    #endregion
}
