namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Represents edge values for UI layout (padding, margin, border offsets).
/// </summary>
/// <param name="Left">Left edge value in pixels.</param>
/// <param name="Top">Top edge value in pixels.</param>
/// <param name="Right">Right edge value in pixels.</param>
/// <param name="Bottom">Bottom edge value in pixels.</param>
public readonly record struct UIEdges(float Left, float Top, float Right, float Bottom)
{
    /// <summary>
    /// An edges value with all sides set to zero.
    /// </summary>
    public static UIEdges Zero => new(0, 0, 0, 0);

    /// <summary>
    /// Creates edges with all sides set to the same value.
    /// </summary>
    /// <param name="value">The value for all sides.</param>
    /// <returns>A new <see cref="UIEdges"/> with uniform values.</returns>
    public static UIEdges All(float value) => new(value, value, value, value);

    /// <summary>
    /// Creates edges with horizontal and vertical values.
    /// </summary>
    /// <param name="horizontal">Value for left and right edges.</param>
    /// <param name="vertical">Value for top and bottom edges.</param>
    /// <returns>A new <see cref="UIEdges"/> with symmetric values.</returns>
    public static UIEdges Symmetric(float horizontal, float vertical) =>
        new(horizontal, vertical, horizontal, vertical);

    /// <summary>
    /// Gets the total horizontal size (left + right).
    /// </summary>
    public float HorizontalSize => Left + Right;

    /// <summary>
    /// Gets the total vertical size (top + bottom).
    /// </summary>
    public float VerticalSize => Top + Bottom;
}
