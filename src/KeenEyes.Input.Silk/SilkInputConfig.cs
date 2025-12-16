namespace KeenEyes.Input.Silk;

/// <summary>
/// Configuration options for the Silk.NET input plugin.
/// </summary>
public sealed class SilkInputConfig
{
    /// <summary>
    /// Gets or sets whether to enable gamepad support.
    /// </summary>
    /// <remarks>
    /// Disabling gamepads can reduce overhead if your game doesn't use them.
    /// </remarks>
    public bool EnableGamepads { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of gamepads to support.
    /// </summary>
    public int MaxGamepads { get; set; } = 4;

    /// <summary>
    /// Gets or sets the deadzone for gamepad analog sticks.
    /// </summary>
    /// <remarks>
    /// Values below this threshold are treated as zero to prevent drift.
    /// </remarks>
    public float GamepadDeadzone { get; set; } = 0.15f;

    /// <summary>
    /// Gets or sets whether to capture mouse on click.
    /// </summary>
    /// <remarks>
    /// When enabled, clicking in the window will capture the mouse cursor.
    /// This is useful for first-person games that need relative mouse input.
    /// </remarks>
    public bool CaptureMouseOnClick { get; set; }
}
