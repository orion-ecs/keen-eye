namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies how text overflow is handled when text exceeds the element bounds.
/// </summary>
public enum TextOverflow : byte
{
    /// <summary>
    /// Text is rendered beyond the element bounds (no clipping).
    /// </summary>
    Visible = 0,

    /// <summary>
    /// Text is clipped at the element bounds.
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// Text is truncated with an ellipsis (...) when it exceeds bounds.
    /// </summary>
    Ellipsis = 2
}
