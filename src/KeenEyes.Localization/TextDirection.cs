namespace KeenEyes.Localization;

/// <summary>
/// Specifies the text direction for a locale.
/// </summary>
/// <remarks>
/// <para>
/// Text direction determines the reading order and layout direction for text and UI elements.
/// Most languages use left-to-right (LTR) direction, but some languages like Arabic, Hebrew,
/// Persian, and Urdu use right-to-left (RTL) direction.
/// </para>
/// <para>
/// When RTL direction is active, UI layouts should mirror horizontally to provide a natural
/// reading experience for RTL language speakers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var locale = Locale.Arabic;
/// var direction = locale.TextDirection; // Returns TextDirection.RightToLeft
///
/// if (direction == TextDirection.RightToLeft)
/// {
///     // Mirror UI layout
/// }
/// </code>
/// </example>
public enum TextDirection : byte
{
    /// <summary>
    /// Left-to-right text direction (default for most languages).
    /// </summary>
    /// <remarks>
    /// Used by Latin, Cyrillic, Greek, CJK, Thai, Hindi, and most other scripts.
    /// </remarks>
    LeftToRight = 0,

    /// <summary>
    /// Right-to-left text direction.
    /// </summary>
    /// <remarks>
    /// Used by Arabic, Hebrew, Persian (Farsi), Urdu, and related scripts.
    /// When this direction is active, UI layouts should typically mirror horizontally.
    /// </remarks>
    RightToLeft = 1
}
