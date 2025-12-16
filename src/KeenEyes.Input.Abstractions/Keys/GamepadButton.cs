namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Gamepad button identifiers following a common controller layout.
/// </summary>
/// <remarks>
/// Based on the XInput/Xbox controller layout, which is the de facto standard
/// for PC game controllers. Backend implementations should map other controller
/// types (PlayStation, Nintendo) to these standard button identifiers.
/// </remarks>
public enum GamepadButton
{
    /// <summary>Unknown or unsupported button.</summary>
    Unknown = 0,

    // Face buttons
    /// <summary>The south face button (Xbox A, PlayStation X, Nintendo B).</summary>
    South = 1,

    /// <summary>The east face button (Xbox B, PlayStation Circle, Nintendo A).</summary>
    East = 2,

    /// <summary>The west face button (Xbox X, PlayStation Square, Nintendo Y).</summary>
    West = 3,

    /// <summary>The north face button (Xbox Y, PlayStation Triangle, Nintendo X).</summary>
    North = 4,

    // Shoulder buttons
    /// <summary>The left shoulder/bumper button.</summary>
    LeftShoulder = 5,

    /// <summary>The right shoulder/bumper button.</summary>
    RightShoulder = 6,

    // Triggers (as digital buttons)
    /// <summary>The left trigger when pressed as a button.</summary>
    LeftTrigger = 7,

    /// <summary>The right trigger when pressed as a button.</summary>
    RightTrigger = 8,

    // D-Pad
    /// <summary>The D-Pad up button.</summary>
    DPadUp = 9,

    /// <summary>The D-Pad down button.</summary>
    DPadDown = 10,

    /// <summary>The D-Pad left button.</summary>
    DPadLeft = 11,

    /// <summary>The D-Pad right button.</summary>
    DPadRight = 12,

    // Thumbstick buttons
    /// <summary>The left thumbstick click (pressing down on the stick).</summary>
    LeftStick = 13,

    /// <summary>The right thumbstick click (pressing down on the stick).</summary>
    RightStick = 14,

    // Menu buttons
    /// <summary>The start/options button.</summary>
    Start = 15,

    /// <summary>The back/select/share button.</summary>
    Back = 16,

    /// <summary>The guide/home/system button.</summary>
    Guide = 17,

    // Additional buttons (for controllers with extra buttons)
    /// <summary>Touchpad click (PlayStation controllers).</summary>
    Touchpad = 18,

    /// <summary>Miscellaneous button 1.</summary>
    Misc1 = 19,

    /// <summary>Miscellaneous button 2.</summary>
    Misc2 = 20
}

/// <summary>
/// Gamepad analog axis identifiers.
/// </summary>
/// <remarks>
/// Represents the analog axes available on standard game controllers.
/// Values are typically normalized to -1.0 to 1.0 for sticks, and 0.0 to 1.0 for triggers.
/// </remarks>
public enum GamepadAxis
{
    /// <summary>Unknown or unsupported axis.</summary>
    Unknown = 0,

    /// <summary>Left thumbstick X axis (-1 left, +1 right).</summary>
    LeftStickX = 1,

    /// <summary>Left thumbstick Y axis (-1 down, +1 up).</summary>
    LeftStickY = 2,

    /// <summary>Right thumbstick X axis (-1 left, +1 right).</summary>
    RightStickX = 3,

    /// <summary>Right thumbstick Y axis (-1 down, +1 up).</summary>
    RightStickY = 4,

    /// <summary>Left analog trigger (0 released, 1 fully pressed).</summary>
    LeftTrigger = 5,

    /// <summary>Right analog trigger (0 released, 1 fully pressed).</summary>
    RightTrigger = 6
}
