using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// A snapshot of mouse state at a specific point in time.
/// </summary>
/// <remarks>
/// <para>
/// This readonly struct captures the complete mouse state for polling-based input.
/// It includes position, button states, and scroll wheel delta.
/// </para>
/// <para>
/// Position values are in window coordinates (pixels from top-left).
/// Scroll wheel values represent the delta since the last frame.
/// </para>
/// </remarks>
/// <param name="Position">The current mouse position in window coordinates.</param>
/// <param name="PressedButtons">Flags indicating which buttons are currently pressed.</param>
/// <param name="ScrollDelta">The scroll wheel delta since the last frame.</param>
public readonly record struct MouseState(
    Vector2 Position,
    MouseButtons PressedButtons,
    Vector2 ScrollDelta)
{
    /// <summary>
    /// An empty mouse state at the origin with no buttons pressed.
    /// </summary>
    public static readonly MouseState Empty = new(Vector2.Zero, MouseButtons.None, Vector2.Zero);

    /// <summary>
    /// Gets the X position of the mouse cursor.
    /// </summary>
    public float X => Position.X;

    /// <summary>
    /// Gets the Y position of the mouse cursor.
    /// </summary>
    public float Y => Position.Y;

    /// <summary>
    /// Gets the horizontal scroll delta.
    /// </summary>
    public float ScrollX => ScrollDelta.X;

    /// <summary>
    /// Gets the vertical scroll delta.
    /// </summary>
    public float ScrollY => ScrollDelta.Y;

    /// <summary>
    /// Gets whether the specified mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is pressed; otherwise, <c>false</c>.</returns>
    public bool IsButtonDown(MouseButton button) => button switch
    {
        MouseButton.Left => (PressedButtons & MouseButtons.Left) != 0,
        MouseButton.Right => (PressedButtons & MouseButtons.Right) != 0,
        MouseButton.Middle => (PressedButtons & MouseButtons.Middle) != 0,
        MouseButton.Button4 => (PressedButtons & MouseButtons.Button4) != 0,
        MouseButton.Button5 => (PressedButtons & MouseButtons.Button5) != 0,
        _ => false
    };

    /// <summary>
    /// Gets whether the specified mouse button is currently released.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is released; otherwise, <c>false</c>.</returns>
    public bool IsButtonUp(MouseButton button) => !IsButtonDown(button);

    /// <summary>
    /// Gets whether the left mouse button is pressed.
    /// </summary>
    public bool IsLeftButtonDown => (PressedButtons & MouseButtons.Left) != 0;

    /// <summary>
    /// Gets whether the right mouse button is pressed.
    /// </summary>
    public bool IsRightButtonDown => (PressedButtons & MouseButtons.Right) != 0;

    /// <summary>
    /// Gets whether the middle mouse button is pressed.
    /// </summary>
    public bool IsMiddleButtonDown => (PressedButtons & MouseButtons.Middle) != 0;

    /// <inheritdoc />
    public override string ToString()
        => $"MouseState(X={X:F1}, Y={Y:F1}, Buttons={PressedButtons})";
}

/// <summary>
/// Flags indicating which mouse buttons are pressed.
/// </summary>
/// <remarks>
/// This flags enum allows efficient storage and checking of multiple button states.
/// </remarks>
[Flags]
public enum MouseButtons
{
    /// <summary>No buttons are pressed.</summary>
    None = 0,

    /// <summary>The left mouse button is pressed.</summary>
    Left = 1,

    /// <summary>The right mouse button is pressed.</summary>
    Right = 2,

    /// <summary>The middle mouse button is pressed.</summary>
    Middle = 4,

    /// <summary>The fourth mouse button is pressed.</summary>
    Button4 = 8,

    /// <summary>The fifth mouse button is pressed.</summary>
    Button5 = 16
}
