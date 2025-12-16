namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Event arguments for gamepad axis change events.
/// </summary>
/// <remarks>
/// Provides information about the axis that changed and its new value.
/// Stick axes range from -1.0 to 1.0, triggers range from 0.0 to 1.0.
/// </remarks>
/// <param name="GamepadIndex">The index of the gamepad (0-based).</param>
/// <param name="Axis">The axis that changed.</param>
/// <param name="Value">The new axis value.</param>
/// <param name="PreviousValue">The previous axis value.</param>
public readonly record struct GamepadAxisEventArgs(
    int GamepadIndex,
    GamepadAxis Axis,
    float Value,
    float PreviousValue)
{
    /// <summary>
    /// Gets the change in value from the previous state.
    /// </summary>
    public float Delta => Value - PreviousValue;

    /// <inheritdoc />
    public override string ToString()
        => $"GamepadAxisEvent(Gamepad={GamepadIndex}, Axis={Axis}, Value={Value:F3})";
}
