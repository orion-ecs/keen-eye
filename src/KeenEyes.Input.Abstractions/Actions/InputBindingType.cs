namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Specifies the type of input source for an <see cref="InputBinding"/>.
/// </summary>
public enum InputBindingType
{
    /// <summary>A keyboard key.</summary>
    Key,

    /// <summary>A mouse button.</summary>
    MouseButton,

    /// <summary>A gamepad button.</summary>
    GamepadButton,

    /// <summary>A gamepad analog axis (stick or trigger).</summary>
    GamepadAxis
}
