using System.Numerics;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// A mock gamepad implementation for testing gamepad-dependent systems.
/// </summary>
/// <remarks>
/// <para>
/// MockGamepad provides two categories of methods:
/// </para>
/// <list type="bullet">
/// <item><b>State methods</b> (SetButtonDown, SetLeftStick): Change state without firing events.
/// Use these for polling-based input tests.</item>
/// <item><b>Simulate methods</b> (SimulateButtonDown, SimulateAxisChange): Change state AND fire events.
/// Use these for event-driven input tests.</item>
/// </list>
/// <para>
/// The mock also tracks vibration settings for verification in tests via
/// <see cref="LastVibration"/> and <see cref="IsVibrating"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var gamepad = new MockGamepad(index: 0);
///
/// // For polling-based tests
/// gamepad.SetLeftStick(1.0f, 0.0f); // Full right
/// Assert.Equal(1.0f, gamepad.LeftStick.X);
///
/// // For event-driven tests
/// bool buttonPressed = false;
/// gamepad.OnButtonDown += args => buttonPressed = true;
/// gamepad.SimulateButtonDown(GamepadButton.South);
/// Assert.True(buttonPressed);
///
/// // Verify vibration was triggered
/// gamepad.SetVibration(0.5f, 0.8f);
/// Assert.True(gamepad.IsVibrating);
/// Assert.Equal((0.5f, 0.8f), gamepad.LastVibration);
/// </code>
/// </example>
/// <param name="index">The gamepad index (0-based).</param>
/// <param name="name">The display name of the gamepad.</param>
/// <param name="connected">Whether the gamepad starts connected.</param>
public sealed class MockGamepad(int index = 0, string name = "Mock Gamepad", bool connected = true) : IGamepad
{
    private bool isConnected = connected;
    private GamepadButtons pressedButtons;
    private Vector2 leftStick;
    private Vector2 rightStick;
    private float leftTrigger;
    private float rightTrigger;
    private (float left, float right) vibration;

    #region State Control Methods

    /// <summary>
    /// Sets whether the gamepad is connected without firing events.
    /// </summary>
    /// <param name="connected">The new connection state.</param>
    public void SetConnected(bool connected)
    {
        isConnected = connected;
    }

    /// <summary>
    /// Sets a button as pressed without firing events.
    /// </summary>
    /// <param name="button">The button to press.</param>
    public void SetButtonDown(GamepadButton button)
    {
        pressedButtons |= ButtonToFlag(button);
    }

    /// <summary>
    /// Sets a button as released without firing events.
    /// </summary>
    /// <param name="button">The button to release.</param>
    public void SetButtonUp(GamepadButton button)
    {
        pressedButtons &= ~ButtonToFlag(button);
    }

    /// <summary>
    /// Sets the left analog stick position without firing events.
    /// </summary>
    /// <param name="x">X position (-1 to 1).</param>
    /// <param name="y">Y position (-1 to 1).</param>
    public void SetLeftStick(float x, float y)
    {
        leftStick = new Vector2(Math.Clamp(x, -1f, 1f), Math.Clamp(y, -1f, 1f));
    }

    /// <summary>
    /// Sets the right analog stick position without firing events.
    /// </summary>
    /// <param name="x">X position (-1 to 1).</param>
    /// <param name="y">Y position (-1 to 1).</param>
    public void SetRightStick(float x, float y)
    {
        rightStick = new Vector2(Math.Clamp(x, -1f, 1f), Math.Clamp(y, -1f, 1f));
    }

    /// <summary>
    /// Sets the left trigger value without firing events.
    /// </summary>
    /// <param name="value">Trigger value (0 to 1).</param>
    public void SetLeftTrigger(float value)
    {
        leftTrigger = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Sets the right trigger value without firing events.
    /// </summary>
    /// <param name="value">Trigger value (0 to 1).</param>
    public void SetRightTrigger(float value)
    {
        rightTrigger = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Resets all inputs to their default values.
    /// </summary>
    public void ClearAllInputs()
    {
        pressedButtons = GamepadButtons.None;
        leftStick = Vector2.Zero;
        rightStick = Vector2.Zero;
        leftTrigger = 0f;
        rightTrigger = 0f;
        vibration = (0f, 0f);
    }

    #endregion

    #region Event Simulation Methods

    /// <summary>
    /// Simulates pressing a button, updating state and firing the OnButtonDown event.
    /// </summary>
    /// <param name="button">The button to press.</param>
    public void SimulateButtonDown(GamepadButton button)
    {
        pressedButtons |= ButtonToFlag(button);
        OnButtonDown?.Invoke(new GamepadButtonEventArgs(Index, button));
    }

    /// <summary>
    /// Simulates releasing a button, updating state and firing the OnButtonUp event.
    /// </summary>
    /// <param name="button">The button to release.</param>
    public void SimulateButtonUp(GamepadButton button)
    {
        pressedButtons &= ~ButtonToFlag(button);
        OnButtonUp?.Invoke(new GamepadButtonEventArgs(Index, button));
    }

    /// <summary>
    /// Simulates an axis value change, updating state and firing the OnAxisChanged event.
    /// </summary>
    /// <param name="axis">The axis that changed.</param>
    /// <param name="value">The new axis value.</param>
    public void SimulateAxisChange(GamepadAxis axis, float value)
    {
        float previousValue = GetAxis(axis);
        float clampedValue = axis switch
        {
            GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger => Math.Clamp(value, 0f, 1f),
            _ => Math.Clamp(value, -1f, 1f)
        };

        SetAxisValue(axis, clampedValue);
        OnAxisChanged?.Invoke(new GamepadAxisEventArgs(Index, axis, clampedValue, previousValue));
    }

    /// <summary>
    /// Simulates the gamepad connecting, updating state and firing the OnConnected event.
    /// </summary>
    public void SimulateConnect()
    {
        isConnected = true;
        OnConnected?.Invoke(this);
    }

    /// <summary>
    /// Simulates the gamepad disconnecting, updating state and firing the OnDisconnected event.
    /// </summary>
    public void SimulateDisconnect()
    {
        isConnected = false;
        ClearAllInputs();
        OnDisconnected?.Invoke(this);
    }

    #endregion

    #region Vibration Tracking

    /// <summary>
    /// Gets the last vibration values that were set.
    /// </summary>
    /// <remarks>
    /// Use this to verify that game code triggered the expected vibration.
    /// </remarks>
    public (float left, float right) LastVibration => vibration;

    /// <summary>
    /// Gets whether the gamepad is currently vibrating.
    /// </summary>
    public bool IsVibrating => vibration.left > 0f || vibration.right > 0f;

    #endregion

    #region IGamepad Implementation

    /// <inheritdoc />
    public int Index { get; } = index;

    /// <inheritdoc />
    public bool IsConnected => isConnected;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public GamepadState GetState() => new(
        Index,
        isConnected,
        pressedButtons,
        leftStick,
        rightStick,
        leftTrigger,
        rightTrigger);

    /// <inheritdoc />
    public bool IsButtonDown(GamepadButton button) => (pressedButtons & ButtonToFlag(button)) != 0;

    /// <inheritdoc />
    public bool IsButtonUp(GamepadButton button) => !IsButtonDown(button);

    /// <inheritdoc />
    public float GetAxis(GamepadAxis axis) => axis switch
    {
        GamepadAxis.LeftStickX => leftStick.X,
        GamepadAxis.LeftStickY => leftStick.Y,
        GamepadAxis.RightStickX => rightStick.X,
        GamepadAxis.RightStickY => rightStick.Y,
        GamepadAxis.LeftTrigger => leftTrigger,
        GamepadAxis.RightTrigger => rightTrigger,
        _ => 0f
    };

    /// <inheritdoc />
    public Vector2 LeftStick => leftStick;

    /// <inheritdoc />
    public Vector2 RightStick => rightStick;

    /// <inheritdoc />
    public float LeftTrigger => leftTrigger;

    /// <inheritdoc />
    public float RightTrigger => rightTrigger;

    /// <inheritdoc />
    public void SetVibration(float leftMotor, float rightMotor)
    {
        vibration = (Math.Clamp(leftMotor, 0f, 1f), Math.Clamp(rightMotor, 0f, 1f));
    }

    /// <inheritdoc />
    public void StopVibration()
    {
        vibration = (0f, 0f);
    }

    /// <inheritdoc />
    public event Action<GamepadButtonEventArgs>? OnButtonDown;

    /// <inheritdoc />
    public event Action<GamepadButtonEventArgs>? OnButtonUp;

    /// <inheritdoc />
    public event Action<GamepadAxisEventArgs>? OnAxisChanged;

    /// <inheritdoc />
    public event Action<IGamepad>? OnConnected;

    /// <inheritdoc />
    public event Action<IGamepad>? OnDisconnected;

    #endregion

    #region Private Helpers

    private void SetAxisValue(GamepadAxis axis, float value)
    {
        switch (axis)
        {
            case GamepadAxis.LeftStickX:
                leftStick = new Vector2(value, leftStick.Y);
                break;
            case GamepadAxis.LeftStickY:
                leftStick = new Vector2(leftStick.X, value);
                break;
            case GamepadAxis.RightStickX:
                rightStick = new Vector2(value, rightStick.Y);
                break;
            case GamepadAxis.RightStickY:
                rightStick = new Vector2(rightStick.X, value);
                break;
            case GamepadAxis.LeftTrigger:
                leftTrigger = value;
                break;
            case GamepadAxis.RightTrigger:
                rightTrigger = value;
                break;
        }
    }

    private static GamepadButtons ButtonToFlag(GamepadButton button) => button switch
    {
        GamepadButton.South => GamepadButtons.South,
        GamepadButton.East => GamepadButtons.East,
        GamepadButton.West => GamepadButtons.West,
        GamepadButton.North => GamepadButtons.North,
        GamepadButton.LeftShoulder => GamepadButtons.LeftShoulder,
        GamepadButton.RightShoulder => GamepadButtons.RightShoulder,
        GamepadButton.LeftTrigger => GamepadButtons.LeftTrigger,
        GamepadButton.RightTrigger => GamepadButtons.RightTrigger,
        GamepadButton.DPadUp => GamepadButtons.DPadUp,
        GamepadButton.DPadDown => GamepadButtons.DPadDown,
        GamepadButton.DPadLeft => GamepadButtons.DPadLeft,
        GamepadButton.DPadRight => GamepadButtons.DPadRight,
        GamepadButton.LeftStick => GamepadButtons.LeftStick,
        GamepadButton.RightStick => GamepadButtons.RightStick,
        GamepadButton.Start => GamepadButtons.Start,
        GamepadButton.Back => GamepadButtons.Back,
        GamepadButton.Guide => GamepadButtons.Guide,
        _ => GamepadButtons.None
    };

    #endregion
}
