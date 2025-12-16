using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Event arguments for mouse move events.
/// </summary>
/// <remarks>
/// Provides both the absolute position and the delta (change) since the last move event.
/// The delta is particularly useful for camera control and relative mouse movement.
/// </remarks>
/// <param name="Position">The new cursor position in window coordinates.</param>
/// <param name="Delta">The change in position since the last move event.</param>
public readonly record struct MouseMoveEventArgs(
    Vector2 Position,
    Vector2 Delta)
{
    /// <summary>
    /// Gets the X coordinate of the cursor.
    /// </summary>
    public float X => Position.X;

    /// <summary>
    /// Gets the Y coordinate of the cursor.
    /// </summary>
    public float Y => Position.Y;

    /// <summary>
    /// Gets the horizontal movement delta.
    /// </summary>
    public float DeltaX => Delta.X;

    /// <summary>
    /// Gets the vertical movement delta.
    /// </summary>
    public float DeltaY => Delta.Y;

    /// <summary>
    /// Creates move event arguments from position values.
    /// </summary>
    /// <param name="x">The new X position.</param>
    /// <param name="y">The new Y position.</param>
    /// <param name="deltaX">The X movement delta.</param>
    /// <param name="deltaY">The Y movement delta.</param>
    /// <returns>A new <see cref="MouseMoveEventArgs"/> instance.</returns>
    public static MouseMoveEventArgs Create(float x, float y, float deltaX, float deltaY)
        => new(new Vector2(x, y), new Vector2(deltaX, deltaY));

    /// <inheritdoc />
    public override string ToString()
        => $"MouseMoveEvent(X={X:F1}, Y={Y:F1}, DeltaX={DeltaX:F1}, DeltaY={DeltaY:F1})";
}
