using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Event arguments for mouse button events.
/// </summary>
/// <remarks>
/// Provides information about the button that was pressed or released
/// and the cursor position at the time of the event.
/// </remarks>
/// <param name="Button">The button that was pressed or released.</param>
/// <param name="Position">The cursor position when the event occurred.</param>
/// <param name="Modifiers">The keyboard modifier keys held during the event.</param>
public readonly record struct MouseButtonEventArgs(
    MouseButton Button,
    Vector2 Position,
    KeyModifiers Modifiers)
{
    /// <summary>
    /// Gets the X coordinate of the cursor when the event occurred.
    /// </summary>
    public float X => Position.X;

    /// <summary>
    /// Gets the Y coordinate of the cursor when the event occurred.
    /// </summary>
    public float Y => Position.Y;

    /// <summary>
    /// Gets whether Shift was held during this event.
    /// </summary>
    public bool IsShiftDown => (Modifiers & KeyModifiers.Shift) != 0;

    /// <summary>
    /// Gets whether Control was held during this event.
    /// </summary>
    public bool IsControlDown => (Modifiers & KeyModifiers.Control) != 0;

    /// <summary>
    /// Gets whether Alt was held during this event.
    /// </summary>
    public bool IsAltDown => (Modifiers & KeyModifiers.Alt) != 0;

    /// <summary>
    /// Creates event arguments with no modifiers.
    /// </summary>
    /// <param name="button">The button that was pressed or released.</param>
    /// <param name="position">The cursor position.</param>
    /// <returns>A new <see cref="MouseButtonEventArgs"/> instance.</returns>
    public static MouseButtonEventArgs Create(MouseButton button, Vector2 position)
        => new(button, position, KeyModifiers.None);

    /// <inheritdoc />
    public override string ToString()
        => $"MouseButtonEvent({Button}, X={X:F1}, Y={Y:F1})";
}
