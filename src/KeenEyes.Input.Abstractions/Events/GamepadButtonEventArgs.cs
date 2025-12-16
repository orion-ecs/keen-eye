namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Event arguments for gamepad button events.
/// </summary>
/// <remarks>
/// Provides information about the button that was pressed or released
/// and the gamepad index that generated the event.
/// </remarks>
/// <param name="GamepadIndex">The index of the gamepad (0-based).</param>
/// <param name="Button">The button that was pressed or released.</param>
public readonly record struct GamepadButtonEventArgs(
    int GamepadIndex,
    GamepadButton Button)
{
    /// <inheritdoc />
    public override string ToString()
        => $"GamepadButtonEvent(Gamepad={GamepadIndex}, Button={Button})";
}
