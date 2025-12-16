using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// A snapshot of gamepad state at a specific point in time.
/// </summary>
/// <remarks>
/// <para>
/// This readonly struct captures the complete gamepad state for polling-based input.
/// It includes button states, analog stick positions, and trigger values.
/// </para>
/// <para>
/// Stick values range from -1.0 to 1.0, with (0, 0) being the center position.
/// Trigger values range from 0.0 (released) to 1.0 (fully pressed).
/// </para>
/// </remarks>
/// <param name="Index">The gamepad index (0-based).</param>
/// <param name="IsConnected">Whether the gamepad is currently connected.</param>
/// <param name="PressedButtons">Flags indicating which buttons are currently pressed.</param>
/// <param name="LeftStick">The left analog stick position.</param>
/// <param name="RightStick">The right analog stick position.</param>
/// <param name="LeftTrigger">The left trigger value (0.0 to 1.0).</param>
/// <param name="RightTrigger">The right trigger value (0.0 to 1.0).</param>
public readonly record struct GamepadState(
    int Index,
    bool IsConnected,
    GamepadButtons PressedButtons,
    Vector2 LeftStick,
    Vector2 RightStick,
    float LeftTrigger,
    float RightTrigger)
{
    /// <summary>
    /// A disconnected gamepad state.
    /// </summary>
    public static readonly GamepadState Disconnected = new(
        Index: -1,
        IsConnected: false,
        PressedButtons: GamepadButtons.None,
        LeftStick: Vector2.Zero,
        RightStick: Vector2.Zero,
        LeftTrigger: 0f,
        RightTrigger: 0f);

    /// <summary>
    /// Gets whether the specified button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is pressed; otherwise, <c>false</c>.</returns>
    public bool IsButtonDown(GamepadButton button) => button switch
    {
        GamepadButton.South => (PressedButtons & GamepadButtons.South) != 0,
        GamepadButton.East => (PressedButtons & GamepadButtons.East) != 0,
        GamepadButton.West => (PressedButtons & GamepadButtons.West) != 0,
        GamepadButton.North => (PressedButtons & GamepadButtons.North) != 0,
        GamepadButton.LeftShoulder => (PressedButtons & GamepadButtons.LeftShoulder) != 0,
        GamepadButton.RightShoulder => (PressedButtons & GamepadButtons.RightShoulder) != 0,
        GamepadButton.LeftTrigger => (PressedButtons & GamepadButtons.LeftTrigger) != 0,
        GamepadButton.RightTrigger => (PressedButtons & GamepadButtons.RightTrigger) != 0,
        GamepadButton.DPadUp => (PressedButtons & GamepadButtons.DPadUp) != 0,
        GamepadButton.DPadDown => (PressedButtons & GamepadButtons.DPadDown) != 0,
        GamepadButton.DPadLeft => (PressedButtons & GamepadButtons.DPadLeft) != 0,
        GamepadButton.DPadRight => (PressedButtons & GamepadButtons.DPadRight) != 0,
        GamepadButton.LeftStick => (PressedButtons & GamepadButtons.LeftStick) != 0,
        GamepadButton.RightStick => (PressedButtons & GamepadButtons.RightStick) != 0,
        GamepadButton.Start => (PressedButtons & GamepadButtons.Start) != 0,
        GamepadButton.Back => (PressedButtons & GamepadButtons.Back) != 0,
        GamepadButton.Guide => (PressedButtons & GamepadButtons.Guide) != 0,
        _ => false
    };

    /// <summary>
    /// Gets whether the specified button is currently released.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is released; otherwise, <c>false</c>.</returns>
    public bool IsButtonUp(GamepadButton button) => !IsButtonDown(button);

    /// <summary>
    /// Gets the value of the specified axis.
    /// </summary>
    /// <param name="axis">The axis to query.</param>
    /// <returns>The axis value (-1.0 to 1.0 for sticks, 0.0 to 1.0 for triggers).</returns>
    public float GetAxis(GamepadAxis axis) => axis switch
    {
        GamepadAxis.LeftStickX => LeftStick.X,
        GamepadAxis.LeftStickY => LeftStick.Y,
        GamepadAxis.RightStickX => RightStick.X,
        GamepadAxis.RightStickY => RightStick.Y,
        GamepadAxis.LeftTrigger => LeftTrigger,
        GamepadAxis.RightTrigger => RightTrigger,
        _ => 0f
    };

    /// <summary>
    /// Gets the left stick X position (-1.0 to 1.0).
    /// </summary>
    public float LeftStickX => LeftStick.X;

    /// <summary>
    /// Gets the left stick Y position (-1.0 to 1.0).
    /// </summary>
    public float LeftStickY => LeftStick.Y;

    /// <summary>
    /// Gets the right stick X position (-1.0 to 1.0).
    /// </summary>
    public float RightStickX => RightStick.X;

    /// <summary>
    /// Gets the right stick Y position (-1.0 to 1.0).
    /// </summary>
    public float RightStickY => RightStick.Y;

    /// <inheritdoc />
    public override string ToString()
        => IsConnected
            ? $"GamepadState(Index={Index}, Buttons={PressedButtons})"
            : "GamepadState(Disconnected)";
}

/// <summary>
/// Flags indicating which gamepad buttons are pressed.
/// </summary>
/// <remarks>
/// This flags enum allows efficient storage and checking of multiple button states.
/// </remarks>
[Flags]
public enum GamepadButtons : uint
{
    /// <summary>No buttons are pressed.</summary>
    None = 0,

    /// <summary>The south face button (A/X/B).</summary>
    South = 1,

    /// <summary>The east face button (B/Circle/A).</summary>
    East = 2,

    /// <summary>The west face button (X/Square/Y).</summary>
    West = 4,

    /// <summary>The north face button (Y/Triangle/X).</summary>
    North = 8,

    /// <summary>The left shoulder button.</summary>
    LeftShoulder = 16,

    /// <summary>The right shoulder button.</summary>
    RightShoulder = 32,

    /// <summary>The left trigger as a digital button.</summary>
    LeftTrigger = 64,

    /// <summary>The right trigger as a digital button.</summary>
    RightTrigger = 128,

    /// <summary>The D-Pad up button.</summary>
    DPadUp = 256,

    /// <summary>The D-Pad down button.</summary>
    DPadDown = 512,

    /// <summary>The D-Pad left button.</summary>
    DPadLeft = 1024,

    /// <summary>The D-Pad right button.</summary>
    DPadRight = 2048,

    /// <summary>The left stick click.</summary>
    LeftStick = 4096,

    /// <summary>The right stick click.</summary>
    RightStick = 8192,

    /// <summary>The start/options button.</summary>
    Start = 16384,

    /// <summary>The back/select button.</summary>
    Back = 32768,

    /// <summary>The guide/home button.</summary>
    Guide = 65536
}
