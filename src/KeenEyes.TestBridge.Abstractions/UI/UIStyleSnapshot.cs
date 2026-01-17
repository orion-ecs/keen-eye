namespace KeenEyes.TestBridge.UI;

/// <summary>
/// Snapshot of a UI element's style.
/// </summary>
public sealed record UIStyleSnapshot
{
    /// <summary>
    /// Gets the background color red component (0-1).
    /// </summary>
    public required float BackgroundColorR { get; init; }

    /// <summary>
    /// Gets the background color green component (0-1).
    /// </summary>
    public required float BackgroundColorG { get; init; }

    /// <summary>
    /// Gets the background color blue component (0-1).
    /// </summary>
    public required float BackgroundColorB { get; init; }

    /// <summary>
    /// Gets the background color alpha component (0-1).
    /// </summary>
    public required float BackgroundColorA { get; init; }

    /// <summary>
    /// Gets the border width in pixels.
    /// </summary>
    public required float BorderWidth { get; init; }

    /// <summary>
    /// Gets the corner radius for rounded rectangles.
    /// </summary>
    public required float CornerRadius { get; init; }

    /// <summary>
    /// Gets the left padding in pixels.
    /// </summary>
    public required float PaddingLeft { get; init; }

    /// <summary>
    /// Gets the right padding in pixels.
    /// </summary>
    public required float PaddingRight { get; init; }

    /// <summary>
    /// Gets the top padding in pixels.
    /// </summary>
    public required float PaddingTop { get; init; }

    /// <summary>
    /// Gets the bottom padding in pixels.
    /// </summary>
    public required float PaddingBottom { get; init; }
}
