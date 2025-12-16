namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Mouse button identifiers.
/// </summary>
/// <remarks>
/// Represents the standard mouse buttons available on most mice.
/// Additional buttons (Button4, Button5, etc.) support mice with extra buttons.
/// </remarks>
public enum MouseButton
{
    /// <summary>Unknown or unsupported button.</summary>
    Unknown = 0,

    /// <summary>The left mouse button (primary click).</summary>
    Left = 1,

    /// <summary>The right mouse button (secondary click).</summary>
    Right = 2,

    /// <summary>The middle mouse button (scroll wheel click).</summary>
    Middle = 3,

    /// <summary>The fourth mouse button (often back/side button).</summary>
    Button4 = 4,

    /// <summary>The fifth mouse button (often forward/side button).</summary>
    Button5 = 5,

    /// <summary>The sixth mouse button.</summary>
    Button6 = 6,

    /// <summary>The seventh mouse button.</summary>
    Button7 = 7,

    /// <summary>The eighth mouse button.</summary>
    Button8 = 8
}
