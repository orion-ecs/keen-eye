using System.Numerics;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Event arguments for mouse scroll wheel events.
/// </summary>
/// <remarks>
/// <para>
/// Provides scroll delta for both vertical (Y) and horizontal (X) scrolling.
/// Positive Y is typically scroll up/forward, positive X is scroll right.
/// </para>
/// <para>
/// Values are usually in "notches" or "lines" but may vary by platform and mouse.
/// High-resolution scroll wheels may report fractional values.
/// </para>
/// </remarks>
/// <param name="Delta">The scroll delta (X for horizontal, Y for vertical).</param>
/// <param name="Position">The cursor position when the event occurred.</param>
public readonly record struct MouseScrollEventArgs(
    Vector2 Delta,
    Vector2 Position)
{
    /// <summary>
    /// Gets the horizontal scroll delta.
    /// </summary>
    public float DeltaX => Delta.X;

    /// <summary>
    /// Gets the vertical scroll delta (positive = up/forward).
    /// </summary>
    public float DeltaY => Delta.Y;

    /// <summary>
    /// Gets the X coordinate of the cursor when scrolling occurred.
    /// </summary>
    public float X => Position.X;

    /// <summary>
    /// Gets the Y coordinate of the cursor when scrolling occurred.
    /// </summary>
    public float Y => Position.Y;

    /// <summary>
    /// Creates scroll event arguments for vertical scrolling.
    /// </summary>
    /// <param name="deltaY">The vertical scroll amount.</param>
    /// <param name="position">The cursor position.</param>
    /// <returns>A new <see cref="MouseScrollEventArgs"/> instance.</returns>
    public static MouseScrollEventArgs Vertical(float deltaY, Vector2 position)
        => new(new Vector2(0, deltaY), position);

    /// <summary>
    /// Creates scroll event arguments for horizontal scrolling.
    /// </summary>
    /// <param name="deltaX">The horizontal scroll amount.</param>
    /// <param name="position">The cursor position.</param>
    /// <returns>A new <see cref="MouseScrollEventArgs"/> instance.</returns>
    public static MouseScrollEventArgs Horizontal(float deltaX, Vector2 position)
        => new(new Vector2(deltaX, 0), position);

    /// <inheritdoc />
    public override string ToString()
        => $"MouseScrollEvent(DeltaX={DeltaX:F2}, DeltaY={DeltaY:F2})";
}
